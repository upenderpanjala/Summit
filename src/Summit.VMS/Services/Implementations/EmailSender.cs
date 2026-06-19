using System.Net;
using System.Net.Mail;
using Summit.VMS.Services.Interfaces;

namespace Summit.VMS.Services.Implementations;

#pragma warning disable SYSLIB0014 // SmtpClient is used deliberately to avoid extra dependencies

/// <summary>
/// Sends mail via SMTP when <c>Smtp:Host</c> is configured. Otherwise it drops
/// .eml files into <c>Smtp:PickupDirectory</c> so the feature works end-to-end
/// in development without a live mail server.
/// </summary>
public class EmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IConfiguration config, IWebHostEnvironment env, ILogger<EmailSender> logger)
    {
        _config = config;
        _env = env;
        _logger = logger;
    }

    public async Task SendAsync(IEnumerable<string> toAddresses, string subject, string htmlBody)
    {
        var recipients = toAddresses
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (recipients.Count == 0) return;

        var smtp = _config.GetSection("Smtp");
        var from = new MailAddress(
            smtp["FromAddress"] ?? "no-reply@summit.gov",
            smtp["FromName"] ?? "Summit VMS");

        using var message = new MailMessage { From = from, Subject = subject, Body = htmlBody, IsBodyHtml = true };
        foreach (var r in recipients) message.To.Add(r);

        try
        {
            using var client = new SmtpClient();
            var host = smtp["Host"];

            if (string.IsNullOrWhiteSpace(host))
            {
                // Pickup directory fallback (no server required).
                var dir = ResolvePath(smtp["PickupDirectory"] ?? "Storage/mail-drop");
                Directory.CreateDirectory(dir);
                client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                client.PickupDirectoryLocation = dir;
                client.Send(message); // pickup delivery is synchronous
                _logger.LogInformation("Email written to pickup directory {Dir} for {Count} recipient(s).",
                    dir, recipients.Count);
            }
            else
            {
                client.Host = host;
                client.Port = int.TryParse(smtp["Port"], out var p) ? p : 587;
                client.EnableSsl = bool.TryParse(smtp["EnableSsl"], out var ssl) && ssl;
                var user = smtp["User"];
                if (!string.IsNullOrWhiteSpace(user))
                    client.Credentials = new NetworkCredential(user, smtp["Password"]);
                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent via {Host} to {Count} recipient(s).", host, recipients.Count);
            }
        }
        catch (Exception ex)
        {
            // Never let a mail failure break the originating operation.
            _logger.LogError(ex, "Failed to send notification email '{Subject}'.", subject);
        }
    }

    private string ResolvePath(string configured) =>
        Path.IsPathRooted(configured) ? configured : Path.Combine(_env.ContentRootPath, configured);
}

#pragma warning restore SYSLIB0014
