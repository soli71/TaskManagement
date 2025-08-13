using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace TaskManagementMvc.Services
{
    public interface ITelegramSender
    {
        Task<bool> SendMessageAsync(string chatId, string text, CancellationToken ct = default);
    }

    public class TelegramSender : ITelegramSender
    {
        private readonly HttpClient _http;
        private readonly string _botToken;

        public TelegramSender(IConfiguration cfg)
        {
            _http = new HttpClient();
            _botToken = cfg["Telegram:BotToken"] ?? string.Empty;
        }

        public async Task<bool> SendMessageAsync(string chatId, string text, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_botToken) || string.IsNullOrWhiteSpace(chatId)) return false;
            var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
            var payload = new Dictionary<string, string>
            {
                ["chat_id"] = chatId,
                ["text"] = text,
                ["parse_mode"] = "HTML",
                ["disable_web_page_preview"] = "true"
            };
            using var content = new FormUrlEncodedContent(payload);
            var res = await _http.PostAsync(url, content, ct);
            return res.IsSuccessStatusCode;
        }
    }
}
