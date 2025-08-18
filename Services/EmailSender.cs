using Microsoft.Extensions.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace TaskManagementMvc.Services
{
    public interface IEmailSender
    {
        Task SendAsync(string to, string subject, string htmlBody);
    }

    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _cfg;
        public SmtpEmailSender(IConfiguration cfg) => _cfg = cfg;

        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            // configuration
            var host = _cfg["Smtp:Host"] ?? string.Empty;
            var port = int.TryParse(_cfg["Smtp:Port"], out var p) ? p : 587;
            var user = _cfg["Smtp:User"] ?? string.Empty;
            var pass = _cfg["Smtp:Pass"] ?? string.Empty;
            var enableSsl = bool.TryParse(_cfg["Smtp:EnableSsl"], out var ssl) ? ssl : true;
            var timeoutMs = int.TryParse(_cfg["Smtp:TimeoutMs"], out var t) ? t : 10000;
            var fromAddress = _cfg["Smtp:From"] ?? user;
            if (string.IsNullOrWhiteSpace(fromAddress))
                fromAddress = "noreply@localhost";

            var usePickup = bool.TryParse(_cfg["Smtp:UsePickupDirectory"], out var up) && up;
            var pickupDir = _cfg["Smtp:PickupDirectoryLocation"] ?? Path.Combine(AppContext.BaseDirectory, "MailDrop");

            // build message
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(name: null, fromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject ?? string.Empty;
            message.Body = new BodyBuilder { HtmlBody = htmlBody, TextBody = StripHtml(htmlBody) }.ToMessageBody();

            if (usePickup)
            {
                Directory.CreateDirectory(pickupDir);
                // ذخیره ایمیل به عنوان فایل EML
                var fileName = Path.Combine(pickupDir, GenerateUniqueFileName(subject));
                await using var stream = File.Create(fileName);
                await message.WriteToAsync(stream);
                return; // no network send
            }

            using var client = new MailKit.Net.Smtp.SmtpClient();
            client.Timeout = timeoutMs;
            try
            {
                SecureSocketOptions socketOptions = enableSsl ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.Auto;
                await client.ConnectAsync(host, port, socketOptions);

                if (!string.IsNullOrWhiteSpace(user))
                {
                    await client.AuthenticateAsync(user, pass);
                }

                await client.SendAsync(message);
            }
            finally
            {
                if (client.IsConnected)
                {
                    try { await client.DisconnectAsync(true); } catch { /* ignore */ }
                }
            }
        }

        private static string StripHtml(string? html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;
            var span = new string(html.Where(c => !char.IsControl(c)).ToArray());
            // very naive remove tags
            bool inside = false;
            var sb = new System.Text.StringBuilder();
            foreach (var ch in span)
            {
                if (ch == '<') { inside = true; continue; }
                if (ch == '>') { inside = false; continue; }
                if (!inside) sb.Append(ch);
            }
            return sb.ToString();
        }

        private static string GenerateUniqueFileName(string? subject)
        {
            var safe = string.Join("_", (subject ?? "mail").Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            if (safe.Length > 40) safe = safe[..40];
            return $"{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}_{safe}.eml";
        }
    }
}
