using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace CraftMarket.Services;

// SMTP settings, bound from the "Smtp" configuration section. Keep the password in user secrets
// or an environment variable, never in appsettings.json.
public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "noreply@example.com";
}

// Sends the "confirm your order" email.
// Adapted from https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient?view=net-7.0
public class OrderEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<OrderEmailSender> _logger;

    public OrderEmailSender(IOptions<SmtpOptions> options, ILogger<OrderEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    // Returns false when SMTP is not configured or the send fails, so the caller can fall back.
    public async Task<bool> SendOrderConfirmationAsync(string toEmail, string confirmationLink)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogWarning("Smtp:Host is not set, so the order confirmation email was not sent.");
            return false;
        }

        using var smtpClient = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
        };
        if (!string.IsNullOrEmpty(_options.Username))
        {
            smtpClient.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(_options.From),
            Subject = "Confirm your order and payment",
            Body = $@"
                <h1>Thank you for your order!</h1>
                <p>Please confirm your order and payment by clicking the link below:</p>
                <a href='{WebUtility.HtmlEncode(confirmationLink)}'>Confirm Order and Payment</a>",
            IsBodyHtml = true,
        };
        mailMessage.To.Add(toEmail);

        try
        {
            await smtpClient.SendMailAsync(mailMessage);
            return true;
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "Sending the order confirmation email failed.");
            return false;
        }
    }
}
