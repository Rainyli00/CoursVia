using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Hosting;

namespace CoursVia.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public EmailService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var smtpServer = _configuration["EmailSettings:SmtpServer"];
        var portText = _configuration["EmailSettings:Port"];
        var senderName = _configuration["EmailSettings:SenderName"];
        var senderEmail = _configuration["EmailSettings:SenderEmail"];
        var password = _configuration["EmailSettings:Password"];

        if (string.IsNullOrWhiteSpace(smtpServer) ||
            string.IsNullOrWhiteSpace(portText) ||
            string.IsNullOrWhiteSpace(senderName) ||
            string.IsNullOrWhiteSpace(senderEmail) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("EmailSettings bilgileri eksik.");
        }

        int port = int.Parse(portText);

        var email = new MimeMessage();

        email.From.Add(new MailboxAddress(senderName, senderEmail));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = subject;

        email.Body = new BodyBuilder
        {
            HtmlBody = body
        }.ToMessageBody();

        using var smtp = new SmtpClient();

        // Local geliştirme ortamında SMTP sertifika sorunlarını aşmak için.
        // Production ortamında sertifika doğrulaması kapatılmaz.
        if (_environment.IsDevelopment())
        {
            smtp.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
        }

        await smtp.ConnectAsync(smtpServer, port, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(senderEmail, password);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}