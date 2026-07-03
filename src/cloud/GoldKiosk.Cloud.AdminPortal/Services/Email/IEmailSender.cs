namespace GoldKiosk.Cloud.AdminPortal.Services.Email;

/// <summary>
/// Outbound transactional email. Implementations: console (dev), SMTP, SES (prod).
/// Body is plain text + HTML — implementations pick the right MIME parts.
/// </summary>
public interface IEmailSender
{
    /// <summary>Send.</summary>
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}

/// <summary>Email message.</summary>
public sealed class EmailMessage
{
    /// <summary>Gets or sets the to email.</summary>
    public string ToEmail { get; set; } = "";
    /// <summary>Gets or sets the to name.</summary>
    public string ToName { get; set; } = "";
    /// <summary>Gets or sets the subject.</summary>
    public string Subject { get; set; } = "";
    /// <summary>Gets or sets the text body.</summary>
    public string TextBody { get; set; } = "";
    /// <summary>Gets or sets the html body.</summary>
    public string HtmlBody { get; set; } = "";
}
