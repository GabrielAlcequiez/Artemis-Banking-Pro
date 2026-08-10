using ABP.Application.Common.DTOs;
using ABP.Application.Common.Interfaces.Services;
using ABP.Domain.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ABP.Shared.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

       public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }


        public async Task SendAsync(EmailRequestDto emailRequestDto)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName ?? "RealEstateApp", _emailSettings.SenderEmail));
                message.To.Add(new MailboxAddress(!string.IsNullOrWhiteSpace(emailRequestDto.RecipientName) ? emailRequestDto.RecipientName : emailRequestDto.ToEmail, emailRequestDto.ToEmail));
                message.Subject = emailRequestDto.Subject;

                message.Body = new TextPart("html") { Text = emailRequestDto.Body };

                using var client = new SmtpClient();

                var secureOption = _emailSettings.UseSsl
               ? SecureSocketOptions.StartTls
               : SecureSocketOptions.None;

                await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, secureOption);

                if (_emailSettings.RequiresAuthentication)
                    await client.AuthenticateAsync(_emailSettings.SmtpUser, _emailSettings.SmtpPassword);

                await client.SendAsync(message);
                await client.DisconnectAsync(quit: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ocurrió una excepción {ex.Message}, ex");
                throw;
            }
        }
    }
}