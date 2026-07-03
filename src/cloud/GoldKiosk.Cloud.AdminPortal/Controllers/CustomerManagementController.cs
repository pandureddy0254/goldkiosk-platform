using GoldKiosk.Cloud.AdminPortal.Logging;
using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Entities.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldKiosk.Cloud.AdminPortal.Controllers;

/// <summary>Customer management controller.</summary>
[Authorize]
public class CustomerManagementController(
    ICustomerService customerService,
    ITransactionReadService transactionReadService,
    ICurrentUserService currentUser,
    AppDbContext db,
    ILogger<CustomerManagementController> logger) : Controller
{
    // ─────────────────────────── Customer Profiles ─────────────────────────

    /// <summary>Customer details.</summary>
    [HttpGet]
    [Permission("customers:read")]
    public async Task<IActionResult> CustomerDetails(string? search, string? stat, int pageSize = 10, int pageNo = 1, CancellationToken ct = default)
    {
        var models = await customerService.ListAsync(search, stat, pageSize, pageNo, ct);

        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.SearchTerm = search;
        ViewBag.Stat = stat;
        return View(models);
    }

    /// <summary>Add customer.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("customers:write")]
    public async Task<IActionResult> AddCustomer(CustomerMasterViewModel vm, CancellationToken ct = default)
    {
        var result = await customerService.AddAsync(vm, ct);
        if (!result.Success)
        {
            logger.AddCustomerFailed(result.ErrorSummary);
            TempData["CustomerError"] = result.ErrorSummary;
        }
        else
        {
            TempData["CustomerInfo"] = "Customer created.";
        }
        return RedirectToAction(nameof(CustomerDetails));
    }

    /// <summary>Edit customer.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("customers:write")]
    public async Task<IActionResult> EditCustomer(CustomerMasterViewModel vm, CancellationToken ct = default)
    {
        var result = await customerService.EditAsync(vm, ct);
        TempData[result.Success ? "CustomerInfo" : "CustomerError"]
            = result.Success ? $"Customer '{vm.CustomerCode}' updated." : result.ErrorSummary;
        return RedirectToAction(nameof(CustomerDetails));
    }

    /// <summary>Delete customer.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("customers:write")]
    public async Task<IActionResult> DeleteCustomer(string code, CancellationToken ct = default)
    {
        var result = await customerService.DeleteAsync(code, ct);
        TempData[result.Success ? "CustomerInfo" : "CustomerError"]
            = result.Success ? $"Customer '{code}' archived." : result.ErrorSummary;
        return RedirectToAction(nameof(CustomerDetails));
    }

    // ────────────────────────── Transaction History ────────────────────────

    /// <summary>Transaction history.</summary>
    [HttpGet]
    [Permission("customers:read")]
    public async Task<IActionResult> TransactionHistory(string? customerCode, string? search, string? stat, int pageSize = 10, int pageNo = 1, CancellationToken ct = default)
    {
        TransactionHistoryList model = string.IsNullOrWhiteSpace(customerCode)
            ? new TransactionHistoryList { PageSize = pageSize, PageNo = pageNo }
            : await transactionReadService.ListForCustomerAsync(customerCode!, search, stat, pageSize, pageNo, ct);

        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.SearchTerm = search;
        ViewBag.Stat = stat;
        ViewBag.CustomerCode = customerCode;
        return View(model);
    }

    /// <summary>Reverse transaction.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("sales:write")]
    public async Task<IActionResult> ReverseTransaction(Guid transactionId, string? customerCode, CancellationToken ct = default)
    {
        var result = await transactionReadService.ReverseAsync(transactionId, ct);
        TempData[result.Success ? "TransactionInfo" : "TransactionError"]
            = result.Success ? "Reversal raised." : result.ErrorSummary;
        return RedirectToAction(nameof(TransactionHistory), new { customerCode });
    }

    // ─────────────────────────── PII Unmask ────────────────────────────────
    //
    // Requires [Permission("customers:unmask")] and a non-blank reason.
    // Writes a SECURITY-typed audit row BEFORE returning any data, satisfying
    // the "audit-before-return" contract in CLAUDE.md §PII.
    //
    // NOTE: The PII fields on customer.customers (GivenNameEnc, etc.) and
    // customer.customer_contacts (ValueEnc) are AES-encrypted at the application
    // layer. The decryption key store is not wired in this service layer yet —
    // that is a separate infrastructure concern (KeyVault / Envelope-key).
    // This action therefore currently returns a 501 with a documented reason,
    // but it correctly writes the audit row first. Once the key service is
    // injected, replace the NotImplemented branch with the decrypted value.

    /// <summary>Unmask pii.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("customers:unmask")]
    public async Task<IActionResult> UnmaskPii(
        Guid customerId,
        string? field,
        string? reason,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return BadRequest(new { error = "reason is required to unmask PII." });
        }

        if (string.IsNullOrWhiteSpace(field))
        {
            return BadRequest(new { error = "field is required." });
        }

        if (currentUser.TenantId is not Guid tenantId || currentUser.UserId is not Guid actorId)
        {
            return Forbid();
        }

        // Write the audit row FIRST — before returning any data.
        var evt = new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ActorType = "user",
            ActorUserId = actorId,
            ActorLabel = currentUser.DisplayName,
            LogType = "SECURITY",
            Activity = "customers.unmask",
            Module = "customers",
            SubModule = "unmask",
            TargetType = "customer",
            TargetId = customerId,
            AfterJson = System.Text.Json.JsonSerializer.Serialize(new { field, reason }),
            OccurredAt = DateTimeOffset.UtcNow,
        };

        db.AuditEvents.Add(evt);
        await db.SaveChangesAsync(ct);

        logger.PiiUnmasked(customerId, field, actorId, reason);

        // Decryption key service not yet wired (application-layer encryption).
        // Return 501 so the caller knows the audit row was written but the
        // decrypted value cannot yet be returned. Remove this branch and return
        // the actual value once ICustomerEncryptionService is available.
        return StatusCode(501, new
        {
            error = "PII decryption key service is not yet wired. Audit row written. " +
                    "Contact the platform team to complete the key-service integration."
        });
    }
}
