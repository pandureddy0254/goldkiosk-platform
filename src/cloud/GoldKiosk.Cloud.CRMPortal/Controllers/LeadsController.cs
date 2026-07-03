using GoldKiosk.Cloud.CRMPortal.Data;
using GoldKiosk.Cloud.CRMPortal.Models.Domain;
using GoldKiosk.Cloud.CRMPortal.Models.ViewModels;
using GoldKiosk.Cloud.CRMPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace GoldKiosk.Cloud.CRMPortal.Controllers;

/// <summary>Lead pipeline: list, kanban, detail, notes, stage moves, and proposal PDFs.</summary>
public class LeadsController : Controller
{
    private readonly CrmDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IProposalDocumentService _pdf;
    private readonly ProposalOptions _proposalOpts;

    /// <summary>Initializes the controller with its collaborators.</summary>
    /// <param name="db">CRM database context.</param>
    /// <param name="currentUser">Per-request user identity for rep scoping.</param>
    /// <param name="pdf">Proposal PDF renderer.</param>
    /// <param name="proposalOpts">Proposal pricing defaults.</param>
    public LeadsController(
        CrmDbContext db,
        ICurrentUserService currentUser,
        IProposalDocumentService pdf,
        IOptions<ProposalOptions> proposalOpts)
    {
        ArgumentNullException.ThrowIfNull(proposalOpts);

        _db = db;
        _currentUser = currentUser;
        _pdf = pdf;
        _proposalOpts = proposalOpts.Value;
    }

    // Managers see all leads; a rep sees only leads they own (leads_app_select).
    private IQueryable<Lead> ScopedLeads()
    {
        var q = _db.Leads.AsNoTracking().Where(l => l.DeletedAt == null);
        if (!_currentUser.IsManager && _currentUser.UserId is Guid uid)
        {
            q = q.Where(l => l.OwnerId == uid);
        }

        return q;
    }

    // -----------------------------------------------------------------------
    // Shared loader — used by both Index and Kanban so they show the same set.
    // -----------------------------------------------------------------------
    private async Task<LeadListViewModel> LoadAsync(string? stage, string? source, string? owner, string? q, CancellationToken ct)
    {
        var query = ScopedLeads();

        if (!string.IsNullOrWhiteSpace(stage) && !string.Equals(stage, "all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(l => l.Stage == stage);
        }

        if (!string.IsNullOrWhiteSpace(source) && !string.Equals(source, "all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(l => l.Source == source);
        }

        if (!string.IsNullOrWhiteSpace(owner) && Guid.TryParse(owner, out var ownerGuid))
        {
            query = query.Where(l => l.OwnerId == ownerGuid);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var like = $"%{q}%";
            query = query.Where(l =>
                EF.Functions.ILike(l.CompanyName, like) ||
                EF.Functions.ILike(l.ContactName, like) ||
                (l.ContactEmail != null && EF.Functions.ILike(l.ContactEmail, like)));
        }

        var leads = await query.OrderByDescending(l => l.CreatedAt).ToListAsync(ct);

        // Pull the owner profiles in one go so we can render initials/names.
        var owners = new Dictionary<Guid, Profile>();
        var ownerIds = leads.Where(l => l.OwnerId is not null).Select(l => l.OwnerId!.Value).Distinct().ToList();
        if (ownerIds.Count > 0)
        {
            owners = await _db.Profiles.AsNoTracking()
                .Where(p => ownerIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, ct);
        }

        return new LeadListViewModel
        {
            Leads = leads,
            Owners = owners,
            Stage = stage,
            Source = source,
            OwnerId = owner,
            Query = q,
        };
    }

    /// <summary>Lead list with stage/source/owner/text filters.</summary>
    /// <param name="stage">Stage filter or "all".</param>
    /// <param name="source">Source filter or "all".</param>
    /// <param name="owner">Owner profile id filter.</param>
    /// <param name="q">Free-text search over company/contact/email.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<IActionResult> Index(string? stage, string? source, string? owner, string? q, CancellationToken ct)
    {
        ViewData["Title"] = "Leads";
        ViewData["ActiveSection"] = "leads";
        var vm = await LoadAsync(stage, source, owner, q, ct);
        return View(vm);
    }

    /// <summary>Kanban board over the same scoped set as <see cref="Index"/>.</summary>
    /// <param name="stage">Stage filter or "all".</param>
    /// <param name="source">Source filter or "all".</param>
    /// <param name="owner">Owner profile id filter.</param>
    /// <param name="q">Free-text search over company/contact/email.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<IActionResult> Kanban(string? stage, string? source, string? owner, string? q, CancellationToken ct)
    {
        ViewData["Title"] = "Leads · Kanban";
        ViewData["ActiveSection"] = "leads";
        var vm = await LoadAsync(stage, source, owner, q, ct);
        return View(vm);
    }

    /// <summary>Lead detail page with the activity timeline.</summary>
    /// <param name="id">Lead id.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Lead detail";
        ViewData["ActiveSection"] = "leads";

        var vm = await LoadLeadDetailAsync(id, ct);
        if (vm is null)
        {
            return NotFound();
        }

        return View(vm);
    }

    /// <summary>Posts a free-text note into lead_activities.</summary>
    /// <param name="id">Lead id.</param>
    /// <param name="text">Note body.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddNote(Guid id, string text, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        var row = new LeadActivity
        {
            LeadId = id,
            ActorId = _currentUser.UserId,
            Type = ActivityType.Note,
            Payload = new JObject { ["text"] = text.Trim() },
        };
        _db.LeadActivities.Add(row);
        await _db.SaveChangesAsync(ct);

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Partial-view variant of <see cref="Details"/> for the kanban drawer.</summary>
    /// <param name="id">Lead id.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    public async Task<IActionResult> DetailsPartial(Guid id, CancellationToken ct)
    {
        var vm = await LoadLeadDetailAsync(id, ct);
        if (vm is null)
        {
            return NotFound();
        }

        return PartialView("_LeadDetailDrawer", vm);
    }

    /// <summary>
    /// Moves a lead's stage via the move_lead_stage RPC so the stage_change activity
    /// gets written transactionally on the server (it re-asserts ownership too).
    /// AJAX path: if the request carries X-Requested-With: fetch (set by
    /// leads-kanban.js), returns JSON {id, stage} instead of redirecting.
    /// </summary>
    /// <param name="id">Lead id.</param>
    /// <param name="newStage">Target <see cref="LeadStage"/> value.</param>
    /// <param name="note">Optional note recorded with the move.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveStage(Guid id, string newStage, string? note, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(newStage))
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        // newStage is passed as plain text — the RPC param is crm.lead_stage which
        // is being converted to text. Pass the string straight through.
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT crm.move_lead_stage({id}, {newStage}, {note})", ct);

        bool isAjax = Request.Headers.XRequestedWith == "fetch"
                   || (Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase)
                       && !Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase));
        if (isAjax)
        {
            return Ok(new { id, stage = newStage });
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // -----------------------------------------------------------------------
    // LoadLeadDetailAsync — shared helper for Details + DetailsPartial.
    // -----------------------------------------------------------------------
    private async Task<LeadDetailViewModel?> LoadLeadDetailAsync(Guid id, CancellationToken ct)
    {
        var lead = await ScopedLeads().FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lead is null)
        {
            return null;
        }

        var activities = await _db.LeadActivities.AsNoTracking()
            .Where(a => a.LeadId == id)
            .OrderBy(a => a.OccurredAt)
            .ToListAsync(ct);

        var actorIds = activities.Where(a => a.ActorId is not null).Select(a => a.ActorId!.Value).ToList();
        if (lead.OwnerId is not null)
        {
            actorIds.Add(lead.OwnerId.Value);
        }

        actorIds = actorIds.Distinct().ToList();

        var actors = new Dictionary<Guid, Profile>();
        if (actorIds.Count > 0)
        {
            actors = await _db.Profiles.AsNoTracking()
                .Where(p => actorIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, ct);
        }

        return new LeadDetailViewModel
        {
            Lead = lead,
            Activities = activities,
            Actors = actors,
            Owner = lead.OwnerId is not null && actors.TryGetValue(lead.OwnerId.Value, out var o) ? o : null,
        };
    }

    /// <summary>Creates a lead from the Add Lead modal.</summary>
    /// <param name="company">Company name (required).</param>
    /// <param name="contact">Contact name (required).</param>
    /// <param name="email">Contact email.</param>
    /// <param name="phone">Contact phone.</param>
    /// <param name="source">Lead source (required).</param>
    /// <param name="owner">Owner profile id, or "self".</param>
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
        string? owner,
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
            TempData["LeadError"] = "Company, contact, and source are required.";
            return RedirectToAction(nameof(Index));
        }

        // 'self' is the convention used by the modal — map it to the signed-in user.
        Guid? ownerId = null;
        if (string.Equals(owner, "self", StringComparison.OrdinalIgnoreCase))
        {
            ownerId = _currentUser.UserId;
        }
        else if (!string.IsNullOrWhiteSpace(owner) && Guid.TryParse(owner, out var g))
        {
            ownerId = g;
        }

        // Reps can only own their own leads. Managers can assign to any rep.
        if (!_currentUser.IsManager)
        {
            ownerId = _currentUser.UserId;
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
            Stage = LeadStage.New,
            EstimatedValue = value,
            CurrencyCode = currencyCode,
            EstimatedKioskCount = kiosks,
            OwnerId = ownerId,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
        };

        _db.Leads.Add(lead);
        await _db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Returns the generated proposal PDF; query scoping guards access.</summary>
    /// <param name="id">Lead id.</param>
    /// <param name="download">1 = attachment download, otherwise inline.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [Route("Leads/{id:guid}/proposal.pdf")]
    public async Task<IActionResult> ProposalPdf(Guid id, [FromQuery] int download, CancellationToken ct)
    {
        var lead = await ScopedLeads().FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lead is null)
        {
            return NotFound();
        }

        var opts = new ProposalRenderOptions
        {
            UnitPrice = _proposalOpts.DefaultUnitPriceUsd,
            Currency = lead.CurrencyCode ?? "USD",
            KioskCount = lead.EstimatedKioskCount ?? 1,
            ScopeSummary = lead.ScopeSummary ?? "",
            Deliverables = lead.Deliverables ?? "",
            PaymentTerms = lead.PaymentTerms ?? "",
            ValidityDays = lead.ValidityDays,
        };

        var bytes = _pdf.Render(lead, opts);
        var shortId = id.ToString("N")[..8];
        var fileName = $"proposal-{shortId}.pdf";

        if (download == 1)
        {
            return File(bytes, "application/pdf", fileName);
        }

        Response.Headers.ContentDisposition = $"inline; filename=\"{fileName}\"";
        return File(bytes, "application/pdf");
    }

    /// <summary>HTML wrapper that embeds the proposal PDF in an iframe.</summary>
    /// <param name="id">Lead id.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [Route("Leads/{id:guid}/proposal")]
    public async Task<IActionResult> ProposalView(Guid id, CancellationToken ct)
    {
        var lead = await ScopedLeads().FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lead is null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"Proposal — {lead.CompanyName}";
        ViewData["ActiveSection"] = "leads";
        return View("ProposalView", lead);
    }
}
