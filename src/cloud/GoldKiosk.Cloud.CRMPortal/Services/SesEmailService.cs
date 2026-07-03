using System.Globalization;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using GoldKiosk.Cloud.CRMPortal.Logging;
using Microsoft.Extensions.Options;

namespace GoldKiosk.Cloud.CRMPortal.Services;

/// <summary>
/// Binds the <c>Ses</c> configuration section. Access keys arrive via user-secrets
/// (dev) / Key Vault (prod) — never committed.
/// </summary>
public class SesOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Ses";

    /// <summary>AWS region system name (e.g. us-east-1).</summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>AWS access key id. Secret — user-secrets / Key Vault only.</summary>
    public string AccessKeyId { get; set; } = "";

    /// <summary>AWS secret access key. Secret — user-secrets / Key Vault only.</summary>
    public string SecretAccessKey { get; set; } = "";

    /// <summary>Verified sender address.</summary>
    public string FromEmail { get; set; } = "";

    /// <summary>Sender display name.</summary>
    public string FromName { get; set; } = "Gold Kiosk";

    /// <summary>Optional reply-to address.</summary>
    public string ReplyTo { get; set; } = "";
}

/// <summary>Outcome of an email send.</summary>
/// <param name="Success">True when SES accepted the message.</param>
/// <param name="MessageId">SES message id on success.</param>
/// <param name="Error">Failure detail when <paramref name="Success"/> is false.</param>
public record EmailSendResult(bool Success, string? MessageId, string? Error);

/// <summary>Sends transactional CRM email (license delivery).</summary>
public interface IEmailService
{
    /// <summary>Emails a freshly issued license token to the partner's primary admin.</summary>
    /// <param name="toEmail">Recipient address.</param>
    /// <param name="toName">Recipient display name for the greeting.</param>
    /// <param name="payload">The signed license payload (for the summary block).</param>
    /// <param name="activationToken">The cleartext activation token.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<EmailSendResult> SendLicenseEmailAsync(
        string toEmail,
        string toName,
        LicensePayload payload,
        string activationToken,
        CancellationToken ct = default);
}

/// <summary>
/// AWS SES v2 implementation of <see cref="IEmailService"/>. Owns the SES client and
/// disposes it with the service (registered as a singleton, so the container disposes
/// it at shutdown).
/// </summary>
public sealed class SesEmailService : IEmailService, IDisposable
{
    private readonly SesOptions _opts;
    private readonly ILogger<SesEmailService> _log;
    private readonly AmazonSimpleEmailServiceV2Client _client;

    /// <summary>Initializes the SES client from configuration.</summary>
    /// <param name="opts">Bound <see cref="SesOptions"/> (keys from user-secrets/Key Vault).</param>
    /// <param name="log">Logger (recipient addresses are never written to it).</param>
    public SesEmailService(IOptions<SesOptions> opts, ILogger<SesEmailService> log)
    {
        _opts = opts.Value;
        _log = log;

        var creds = new BasicAWSCredentials(_opts.AccessKeyId, _opts.SecretAccessKey);
        _client = new AmazonSimpleEmailServiceV2Client(creds, RegionEndpoint.GetBySystemName(_opts.Region));
    }

    /// <inheritdoc/>
    public async Task<EmailSendResult> SendLicenseEmailAsync(
        string toEmail,
        string toName,
        LicensePayload payload,
        string activationToken,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return new EmailSendResult(false, null, "Recipient email is empty.");
        }

        var subject = $"Your Gold Kiosk license · {payload.LegalName}";
        var html = BuildHtmlBody(toName, payload, activationToken);
        var text = BuildTextBody(toName, payload, activationToken);
        var fromAddr = $"\"{_opts.FromName}\" <{_opts.FromEmail}>";

        var req = new SendEmailRequest
        {
            FromEmailAddress = fromAddr,
            Destination = new Destination { ToAddresses = [toEmail] },
            ReplyToAddresses = string.IsNullOrWhiteSpace(_opts.ReplyTo)
                ? []
                : [_opts.ReplyTo],
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content { Charset = "UTF-8", Data = subject },
                    Body = new Body
                    {
                        Html = new Content { Charset = "UTF-8", Data = html },
                        Text = new Content { Charset = "UTF-8", Data = text },
                    }
                }
            }
        };

        try
        {
            var resp = await _client.SendEmailAsync(req, ct).ConfigureAwait(false);
            // Log ids only — never the recipient email address (PII stays out of logs).
            _log.LicenseEmailSent(payload.PartnerId, payload.TenantId, resp.MessageId);
            return new EmailSendResult(true, resp.MessageId, null);
        }
        catch (Exception ex)
        {
            // Log ids only — never the recipient email address (PII stays out of logs).
            _log.LicenseEmailSendFailed(ex, payload.PartnerId, payload.TenantId);
            return new EmailSendResult(false, null, ex.Message);
        }
    }

    /// <summary>Disposes the owned SES client.</summary>
    public void Dispose() => _client.Dispose();

    // ── Body templates ─────────────────────────────────────────────────────────

    private static string BuildTextBody(string toName, LicensePayload p, string token)
    {
        var expires = DateTimeOffset.FromUnixTimeSeconds(p.ExpiresAt).ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
        var features = p.Features.Length == 0 ? "(none)" : string.Join(", ", p.Features);
        return $@"Hello {toName},

Your Gold Kiosk Admin Dashboard license has been issued.

  Licensed to: {p.LegalName}
  Plan:        {p.PlanCode}
  Kiosk cap:   {p.KioskCap}
  Features:    {features}
  Expires:     {expires}

To activate, sign in to your Gold Kiosk Admin Dashboard and paste the
following activation key on the Activate screen:

{token}

This key contains your licensing terms in a tamper-evident form and is
verified offline by your Admin Dashboard — there is no need to contact us
to complete activation.

If you need help, reply to this email or write to support@goldkiosk.com.

— The Gold Kiosk team
https://goldkiosk.com
";
    }

    private static string BuildHtmlBody(string toName, LicensePayload p, string token)
    {
        var expires = DateTimeOffset.FromUnixTimeSeconds(p.ExpiresAt).ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
        var features = p.Features.Length == 0 ? "<em>none</em>" : string.Join(" · ", p.Features);
        var safeName = System.Net.WebUtility.HtmlEncode(toName);
        var safeCo = System.Net.WebUtility.HtmlEncode(p.LegalName);
        var safePlan = System.Net.WebUtility.HtmlEncode(p.PlanCode);
        var safeToken = System.Net.WebUtility.HtmlEncode(token);

        return $@"<!doctype html>
<html><body style=""margin:0;padding:24px;background:#faf7f2;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;color:#1c1815;"">
  <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width:600px;margin:0 auto;background:#ffffff;border:1px solid #ece7df;border-radius:12px;overflow:hidden;"">
    <tr><td style=""padding:24px 28px;border-bottom:1px solid #ece7df;"">
      <div style=""font-size:11px;letter-spacing:0.18em;text-transform:uppercase;color:#a08a6a;"">Gold Kiosk · License delivery</div>
      <h1 style=""margin:8px 0 0 0;font-weight:600;font-size:22px;color:#1c1815;"">Your license is ready, {safeName}.</h1>
    </td></tr>

    <tr><td style=""padding:24px 28px;font-size:14px;line-height:1.6;color:#3f372f;"">
      Your Gold Kiosk Admin Dashboard license has been issued. Paste the activation key below into the
      <strong>Activate</strong> screen of your Admin Dashboard to start.
    </td></tr>

    <tr><td style=""padding:0 28px 24px 28px;"">
      <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background:#f5efe4;border:1px solid #e2d8c4;border-radius:8px;"">
        <tr><td style=""padding:14px 16px;"">
          <div style=""font-size:11px;letter-spacing:0.14em;text-transform:uppercase;color:#a08a6a;margin-bottom:6px;"">Licensed to</div>
          <div style=""font-size:15px;font-weight:600;color:#1c1815;"">{safeCo}</div>
          <div style=""font-size:13px;color:#6c6056;margin-top:4px;"">Plan · {safePlan} &nbsp;·&nbsp; Kiosk cap · {p.KioskCap} &nbsp;·&nbsp; Expires · {expires}</div>
          <div style=""font-size:13px;color:#6c6056;margin-top:2px;"">Features · {features}</div>
        </td></tr>
      </table>
    </td></tr>

    <tr><td style=""padding:0 28px 28px 28px;"">
      <div style=""font-size:11px;letter-spacing:0.14em;text-transform:uppercase;color:#a08a6a;margin-bottom:6px;"">Activation key · copy this whole string</div>
      <pre style=""margin:0;padding:14px 16px;background:#1c1815;color:#f5efe4;border-radius:8px;font-family:ui-monospace,'SF Mono',Menlo,Consolas,monospace;font-size:12px;line-height:1.55;white-space:pre-wrap;word-break:break-all;"">{safeToken}</pre>
      <div style=""font-size:12px;color:#6c6056;margin-top:8px;"">This key contains your licensing terms in a tamper-evident form. Your Admin Dashboard verifies it locally — no call back to us is required.</div>
    </td></tr>

    <tr><td style=""padding:18px 28px;border-top:1px solid #ece7df;font-size:12px;color:#6c6056;"">
      Need help? Reply to this email or write to <a href=""mailto:support@goldkiosk.com"" style=""color:#a07f3f;text-decoration:none;"">support@goldkiosk.com</a>.<br/>
      — The Gold Kiosk team · <a href=""https://goldkiosk.com"" style=""color:#a07f3f;text-decoration:none;"">goldkiosk.com</a>
    </td></tr>
  </table>
</body></html>";
    }
}
