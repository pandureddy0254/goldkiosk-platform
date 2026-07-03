using GoldKiosk.Cloud.AdminPortal.Logging;

namespace GoldKiosk.Cloud.AdminPortal.Services.Email;

/// <summary>
/// Dev/demo email sender — writes the message to the logger so the user can
/// copy/paste the invite link. Production should swap this for SES/SMTP in
/// Program.cs via configuration.
/// </summary>
public sealed class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    /// <summary>Initializes a new instance of the <see cref="ConsoleEmailSender"/> class.</summary>
    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) => _logger = logger;

    /// <summary>Send.</summary>
    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        _logger.ConsoleEmailWritten(message.Subject, message.TextBody);
        return Task.CompletedTask;
    }
}
