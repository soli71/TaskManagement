using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

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
            var host = _cfg["Smtp:Host"] ?? string.Empty;
            var port = int.TryParse(_cfg["Smtp:Port"], out var p) ? p : 587;
            var user = _cfg["Smtp:User"] ?? string.Empty;
            var pass = _cfg["Smtp:Pass"] ?? string.Empty;
            var enableSsl = bool.TryParse(_cfg["Smtp:EnableSsl"], out var ssl) ? ssl : true;
            var timeoutMs = int.TryParse(_cfg["Smtp:TimeoutMs"], out var t) ? t : 10000;

            var usePickup = bool.TryParse(_cfg["Smtp:UsePickupDirectory"], out var up) && up;
            var pickupDir = _cfg["Smtp:PickupDirectoryLocation"] ?? Path.Combine(AppContext.BaseDirectory, "MailDrop");

            using var client = new SmtpClient();
            if (usePickup)
            {
                Directory.CreateDirectory(pickupDir);
                client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                client.PickupDirectoryLocation = pickupDir;
            }
            else
            {
                client.Host = host;
                client.Port = port;
                client.EnableSsl = enableSsl;
                if (!string.IsNullOrWhiteSpace(user))
                {
                    client.Credentials = new NetworkCredential(user, pass);
                }
            }

            client.Timeout = timeoutMs;

            using var msg = new MailMessage(string.IsNullOrWhiteSpace(user) ? (user ?? "noreply@localhost") : user, to, subject, htmlBody)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(msg);
        }
    }
}
