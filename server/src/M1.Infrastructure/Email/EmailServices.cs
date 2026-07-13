using System.Net;
using System.Net.Mail;
using M1.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace M1.Infrastructure.Email;

/// <summary>SMTP sender (Brevo/Gmail/any). Selected when Email:Host is configured.</summary>
public class SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        using var client = new SmtpClient(config["Email:Host"], int.Parse(config["Email:Port"] ?? "587"))
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(config["Email:User"], config["Email:Password"])
        };

        using var message = new MailMessage(
            new MailAddress(config["Email:From"] ?? "no-reply@m1stores.com", "M1 Stores"),
            new MailAddress(to))
        {
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        await client.SendMailAsync(message, ct);
        logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
    }
}

/// <summary>
/// Fallback when no SMTP is configured: logs the email instead of failing,
/// so registration and password-reset flows still work in dev/demo.
/// </summary>
public class LogEmailService(ILogger<LogEmailService> logger) : IEmailService
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        logger.LogInformation("EMAIL (log sink) to {To} — {Subject}\n{Body}", to, subject, htmlBody);
        return Task.CompletedTask;
    }
}
