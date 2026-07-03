using GoldKiosk.Cloud.CRMPortal.Data;
using GoldKiosk.Cloud.CRMPortal.Models.Domain;
using GoldKiosk.Cloud.CRMPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.CRMPortal.Controllers;

/// <summary>Contracts register: list/filter plus manual contract recording.</summary>
public class ContractsController : Controller
{
    private readonly CrmDbContext _db;
    private readonly ICurrentUserService _currentUser;

    /// <summary>Initializes the controller with its collaborators.</summary>
    /// <param name="db">CRM database context.</param>
    /// <param name="currentUser">Per-request user identity for rep scoping.</param>
    public ContractsController(CrmDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Lists contracts with partner-name/type/status filters.</summary>
    /// <param name="q">Partner-name search.</param>
    /// <param name="type">Contract-type filter or "all".</param>
    /// <param name="status">Status filter or "all".</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<IActionResult> Index(string? q, string? type, string? status, CancellationToken ct)
    {
        ViewData["Title"] = "Contracts";
        ViewData["ActiveSection"] = "contracts";

        // Pass filter values back so the form re-hydrates on submit.
        ViewBag.Q = q;
        ViewBag.Type = type;
        ViewBag.Status = status;

        // Pull every visible partner (RLS-scoped) so the table can render names
        // AND the New-contract modal has a populated picker.
        var allPartners = await ScopedPartners()
            .OrderBy(p => p.LegalName)
            .ToListAsync(ct);

        var contractsQuery = ScopedContracts();

        // Filter by partner name: find matching partner IDs first, then filter contracts.
        if (!string.IsNullOrWhiteSpace(q))
        {
            var matchingPartnerIds = allPartners
                .Where(p => (p.LegalName?.Contains(q, StringComparison.OrdinalIgnoreCase) == true)
                          || (p.DisplayName?.Contains(q, StringComparison.OrdinalIgnoreCase) == true))
                .Select(p => p.Id)
                .ToList();

            if (matchingPartnerIds.Count > 0)
            {
                contractsQuery = contractsQuery.Where(c => matchingPartnerIds.Contains(c.PartnerId));
            }
            else
            {
                return ReturnEmptyContracts(allPartners, q, type, status);
            }
        }

        if (!string.IsNullOrWhiteSpace(type) && !string.Equals(type, "all", StringComparison.OrdinalIgnoreCase))
        {
            contractsQuery = contractsQuery.Where(c => c.ContractTypeCode == type);
        }

        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
        {
            contractsQuery = contractsQuery.Where(c => c.Status == status);
        }

        var contracts = await contractsQuery
            .OrderByDescending(c => c.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        var partnerLookup = allPartners.ToDictionary(p => p.Id, p => p);
        ViewData["PartnerLookup"] = partnerLookup;
        ViewBag.AllPartners = allPartners;
        return View(contracts);
    }

    private ViewResult ReturnEmptyContracts(List<Partner> allPartners, string? q, string? type, string? status)
    {
        ViewBag.Q = q;
        ViewBag.Type = type;
        ViewBag.Status = status;
        ViewData["PartnerLookup"] = allPartners.ToDictionary(p => p.Id, p => p);
        ViewBag.AllPartners = allPartners;
        return View(new List<Contract>());
    }

    /// <summary>
    /// "Upload PDF / new contract" modal posts here. Inserts a row in crm.contracts in
    /// Draft status. The interceptor sets app.user_id so RLS and audit triggers see the actor.
    /// </summary>
    /// <param name="partnerId">Partner the contract belongs to (required).</param>
    /// <param name="contractType">One of <see cref="ContractType"/> (required).</param>
    /// <param name="status">One of <see cref="ContractStatus"/>; anything else maps to Draft.</param>
    /// <param name="valueAmount">Total contract value.</param>
    /// <param name="currencyCode">ISO currency code for the value.</param>
    /// <param name="termMonths">Fixed term length in months.</param>
    /// <param name="documentUrl">Externally stored document link.</param>
    /// <param name="signedByName">Counterparty signatory name.</param>
    /// <param name="signedByEmail">Counterparty signatory email.</param>
    /// <param name="signedAt">Signature timestamp.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        Guid partnerId,
        string contractType,
        string? status,
        decimal? valueAmount,
        string? currencyCode,
        int? termMonths,
        string? documentUrl,
        string? signedByName,
        string? signedByEmail,
        DateTime? signedAt,
        CancellationToken ct)
    {
        if (partnerId == Guid.Empty || string.IsNullOrWhiteSpace(contractType))
        {
            TempData["ContractError"] = "Partner and contract type are required.";
            return RedirectToAction(nameof(Index));
        }

        // Whitelist the contract type against the crm.contract_type values.
        var validTypes = new[] { ContractType.Msa, ContractType.Sow, ContractType.Dpa, ContractType.Sla, ContractType.Nda, ContractType.Addendum };
        if (!validTypes.Contains(contractType))
        {
            TempData["ContractError"] = "Unsupported contract type.";
            return RedirectToAction(nameof(Index));
        }

        var statusValue = status switch
        {
            ContractStatus.OutForSignature => ContractStatus.OutForSignature,
            ContractStatus.FullySigned => ContractStatus.FullySigned,
            ContractStatus.Terminated => ContractStatus.Terminated,
            _ => ContractStatus.Draft
        };

        var currency = SupportedCurrencies.IsSupported(currencyCode ?? "")
            ? currencyCode!.ToUpperInvariant()
            : "USD";

        var contract = new Contract
        {
            PartnerId = partnerId,
            ContractTypeCode = contractType,
            Status = statusValue,
            ValueAmount = valueAmount,
            CurrencyCode = currency,
            TermMonths = termMonths,
            DocumentUrl = string.IsNullOrWhiteSpace(documentUrl) ? null : documentUrl.Trim(),
            SignedByName = string.IsNullOrWhiteSpace(signedByName) ? null : signedByName.Trim(),
            SignedByEmail = string.IsNullOrWhiteSpace(signedByEmail) ? null : signedByEmail.Trim(),
            SignedAt = statusValue == ContractStatus.FullySigned ? (signedAt ?? DateTime.UtcNow) : signedAt,
        };

        _db.Contracts.Add(contract);
        await _db.SaveChangesAsync(ct);
        TempData["ContractFlash"] = "Contract recorded.";
        return RedirectToAction(nameof(Index));
    }

    private IQueryable<Partner> ScopedPartners()
    {
        var q = _db.Partners.AsNoTracking().AsQueryable();
        if (!_currentUser.IsManager && _currentUser.UserId is Guid uid)
        {
            q = q.Where(p => _db.Leads.Any(l => l.Id == p.LeadId && l.OwnerId == uid));
        }

        return q;
    }

    private IQueryable<Contract> ScopedContracts()
    {
        var q = _db.Contracts.AsNoTracking().AsQueryable();
        if (!_currentUser.IsManager && _currentUser.UserId is Guid uid)
        {
            q = q.Where(c => _db.Partners.Any(p => p.Id == c.PartnerId
                && _db.Leads.Any(l => l.Id == p.LeadId && l.OwnerId == uid)));
        }

        return q;
    }
}
