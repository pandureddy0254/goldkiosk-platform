using System.Globalization;
using GoldKiosk.Cloud.CRMPortal.Data;
using GoldKiosk.Cloud.CRMPortal.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.CRMPortal.Controllers.Api;

/// <summary>Header-bell notifications derived from the audit log.</summary>
[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly CrmDbContext _db;

    /// <summary>Initializes the controller with the CRM database context.</summary>
    /// <param name="db">CRM database context.</param>
    public NotificationsController(CrmDbContext db) => _db = db;

    /// <summary>One notification line in the header dropdown.</summary>
    /// <param name="Id">Audit row id.</param>
    /// <param name="Kind">Raw audit action.</param>
    /// <param name="Title">Humanised headline.</param>
    /// <param name="Body">One-line detail.</param>
    /// <param name="When">ISO-8601 timestamp.</param>
    /// <param name="Unread">Whether the client should badge it.</param>
    public record NotificationItem(
        long Id,
        string Kind,
        string Title,
        string Body,
        string When,
        bool Unread
    );

    /// <summary>
    /// Returns the last 20 audit-log entries as notification items. The audit_log
    /// RLS policy is manager-only, so reps see an empty list automatically.
    /// <para>
    /// Returns { count: 0, items: [] } on any data error rather than 5xx, because
    /// this endpoint is called fire-and-forget on every page load.
    /// </para>
    /// <para>
    /// [IgnoreAntiforgeryToken] is safe here: the request is gated by the
    /// authenticated-user fallback policy, same-origin (SameSite=Lax), and is a
    /// read-only GET with no CSRF surface.
    /// </para>
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-7);

            var entries = await _db.AuditLog.AsNoTracking()
                .Where(e => e.CreatedAt > cutoff)
                .OrderByDescending(e => e.CreatedAt)
                .Take(20)
                .ToListAsync(ct);

            var items = entries.Select(e => new NotificationItem(
                Id: e.Id,
                Kind: e.Action,
                Title: HumaniseAction(e.EntityType, e.Action),
                Body: BuildBody(e),
                When: e.CreatedAt.ToString("O", CultureInfo.InvariantCulture),
                Unread: true
            )).ToList();

            return Ok(new { count = items.Count, items });
        }
        catch (Exception)
        {
            // RLS or table access failure — return empty rather than 5xx.
            return Ok(new { count = 0, items = Array.Empty<object>() });
        }
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static string HumaniseAction(string entityType, string action) =>
        (entityType, action) switch
        {
            (AuditEntityType.Lead, AuditAction.Create) => "Lead created",
            (AuditEntityType.Lead, AuditAction.Update) => "Lead updated",
            (AuditEntityType.Lead, AuditAction.Delete) => "Lead deleted",
            (AuditEntityType.Lead, AuditAction.Provision) => "Lead won",
            (AuditEntityType.Partner, AuditAction.Create) => "Partner created",
            (AuditEntityType.Partner, AuditAction.Update) => "Partner updated",
            (AuditEntityType.ActivationKey, AuditAction.Provision) => "Activation key issued",
            (AuditEntityType.ActivationKey, AuditAction.RevokeKey) => "Activation key revoked",
            (AuditEntityType.ActivationKey, AuditAction.ReissueKey) => "Activation key reissued",
            ("subscription", AuditAction.Create) => "Subscription created",
            (AuditEntityType.Contract, AuditAction.Create) => "Contract created",
            (AuditEntityType.Contract, AuditAction.Update) => "Contract updated",
            (AuditEntityType.Profile, AuditAction.SignIn) => "User signed in",
            _ => Capitalise(action.Replace('_', ' '))
        };

    private static string BuildBody(AuditLogEntry e)
    {
        var snapshot = e.After ?? e.Before;
        if (snapshot is null)
        {
            return $"{Capitalise(e.EntityType)} — {Capitalise(e.Action)}";
        }

        var name =
            snapshot["full_name"]?.ToString()
            ?? snapshot["name"]?.ToString()
            ?? snapshot["company_name"]?.ToString()
            ?? snapshot["email"]?.ToString();

        return name is not null
            ? name
            : $"{Capitalise(e.EntityType)} {e.EntityId?.ToString("N")[..8]}";
    }

    private static string Capitalise(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];
}
