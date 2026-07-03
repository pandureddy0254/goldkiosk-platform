using GoldKiosk.Cloud.CRMPortal.Data;
using GoldKiosk.Cloud.CRMPortal.Models.Domain;
using GoldKiosk.Cloud.CRMPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.CRMPortal.Controllers;

/// <summary>Proposal-stage leads: list and quick-create.</summary>
public class ProposalsController : Controller
{
    private readonly CrmDbContext _db;
    private readonly ICurrentUserService _currentUser;

    /// <summary>Initializes the controller with its collaborators.</summary>
    /// <param name="db">CRM database context.</param>
    /// <param name="currentUser">Per-request user identity for rep scoping.</param>
    public ProposalsController(CrmDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Lists leads currently at the proposal stage.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Proposals";
        ViewData["ActiveSection"] = "proposals";

        var query = _db.Leads.AsNoTracking()
            .Where(l => l.DeletedAt == null && l.Stage == LeadStage.Proposal);

        if (!_currentUser.IsManager && _currentUser.UserId is Guid uid)
        {
            query = query.Where(l => l.OwnerId == uid);
        }

        var proposals = await query
            .OrderByDescending(l => l.UpdatedAt)
            .Take(100)
            .ToListAsync(ct);

        return View(proposals);
    }

    /// <summary>
    /// "New proposal" modal posts here. Inserts a lead directly at stage = 'proposal'
    /// so it appears immediately in the proposals view. Owner is always the signed-in
    /// user (reps can't plant on other reps).
    /// </summary>
    /// <param name="company">Company name (required).</param>
    /// <param name="contact">Contact name (required).</param>
    /// <param name="email">Contact email.</param>
    /// <param name="phone">Contact phone.</param>
    /// <param name="source">Lead source (required).</param>
    /// <param name="country">Country.</param>
    /// <param name="city">Region/city.</param>
    /// <param name="value">Estimated deal value.</param>
    /// <param name="kiosks">Estimated kiosk count.</param>
    /// <param name="notes">Free-form notes.</param>
    /// <param name="currency">Currency code for the estimated value.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string company,
        string contact,
        string? email,
        string? phone,
        string source,
        string? country,
        string? city,
        decimal? value,
        int? kiosks,
        string? notes,
        string? currency,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(company) || string.IsNullOrWhiteSpace(contact) || string.IsNullOrWhiteSpace(source))
        {
            TempData["ProposalError"] = "Company, contact, and source are required.";
            return RedirectToAction(nameof(Index));
        }

        var currencyCode = SupportedCurrencies.IsSupported(currency ?? "")
            ? currency!.ToUpperInvariant()
            : "USD";

        var lead = new Lead
        {
            CompanyName = company.Trim(),
            ContactName = contact.Trim(),
            ContactEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            ContactPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            Country = string.IsNullOrWhiteSpace(country) ? null : country.Trim(),
            Region = string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
            Source = source,
            Stage = LeadStage.Proposal,
            EstimatedValue = value,
            CurrencyCode = currencyCode,
            EstimatedKioskCount = kiosks,
            OwnerId = _currentUser.UserId,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
        };

        _db.Leads.Add(lead);
        await _db.SaveChangesAsync(ct);
        TempData["ProposalFlash"] = "Proposal created.";
        return RedirectToAction(nameof(Index));
    }
}
