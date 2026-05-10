using BookStore.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Shared
{
    public class MailService : IMailService
    {
        private readonly IConfiguration _config;
        public MailService(IConfiguration config) => _config = config;

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_config["MailSettings:Email"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = body };

            using var smtp = new SmtpClient();
            
            // Bỏ qua xác thực Certificate nếu môi trường Hosting yêu cầu (khắc phục lỗi SSL handshake trên Linux/Docker)
            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

            // Kết nối tới server Gmail
            await smtp.ConnectAsync(_config["MailSettings:Host"],
                                   int.Parse(_config["MailSettings:Port"] ?? "587"),
                                   SecureSocketOptions.StartTls);

            // Xác thực tài khoản
            await smtp.AuthenticateAsync(_config["MailSettings:Email"],
                                        _config["MailSettings:Password"]);

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
