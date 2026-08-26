using BookStore.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Shared
{
    public class SendGridMailService : IMailService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public SendGridMailService(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var apiKey = _config["MailSettings:SendGridApiKey"];
            var fromEmail = _config["MailSettings:Email"];
            var fromName = _config["MailSettings:FromName"] ?? "Lumen BookStore";

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("SendGrid API Key chưa được cấu hình (MailSettings:SendGridApiKey).");

            if (string.IsNullOrWhiteSpace(fromEmail))
                throw new InvalidOperationException("Email gửi chưa được cấu hình (MailSettings:Email).");

            var payload = new
            {
                personalizations = new[]
                {
                    new { to = new[] { new { email = toEmail } } }
                },
                from = new { email = fromEmail, name = fromName },
                subject,
                content = new[]
                {
                    new { type = "text/html", value = body }
                }
            };

            var client = _httpClientFactory.CreateClient("SendGrid");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.sendgrid.com/v3/mail/send", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"SendGrid gửi mail thất bại ({(int)response.StatusCode}): {errorBody}");
            }
        }
    }
}
