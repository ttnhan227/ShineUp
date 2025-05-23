using System.Net;
using System.Net.Mail;
using Server.Interfaces;

namespace Server.Services;

public class EmailService : IEmailService
{
    private readonly string _fromEmail;
    private readonly string _password;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _fromEmail = configuration["Gmail:FromEmail"];
        _password = configuration["Gmail:AppPassword"];
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            using var message = new MailMessage(_fromEmail, toEmail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            using var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_fromEmail, _password)
            };

            await smtp.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending email: {ex}");
            throw;
        }
    }
}