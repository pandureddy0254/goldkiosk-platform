using System.Security.Cryptography;
using System.Text;
using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Entities.Customer;
using Microsoft.EntityFrameworkCore;

using CustomerEntity = GoldKiosk.Infrastructure.Entities.Customer.Customer;

namespace GoldKiosk.Cloud.AdminPortal.Services;
/// <summary>
/// CRUD for customer records used by the AdminDashboard Customer Management screens.
/// Wallet operations (top-up / debit / adjustment) post through the
/// <c>customer.post_ledger_entry</c> PL/pgSQL function to keep the balance
/// trigger and the FOR-UPDATE serialisation in one canonical place.
/// </summary>
public sealed class CustomerService(AppDbContext db, ICurrentUserService currentUser) : ICustomerService
{
    /// <summary>List.</summary>
    public async Task<CustomerMasterList> ListAsync(string? search, string? status, int pageSize, int pageNo, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return Empty(pageSize, pageNo);
        }

        var defaultCurrency = await GetTenantDefaultCurrencyAsync(tenantId, ct);

        var baseQ = db.Customers.AsNoTracking().Where(c => c.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            baseQ = baseQ.Where(c => EF.Functions.ILike(c.CustomerCode, s));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            baseQ = baseQ.Where(c => c.Status == status);
        }

        var totals = await db.Customers.AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(c => c.Status == "active"),
                InActive = g.Count(c => c.Status != "active"),
            })
            .FirstOrDefaultAsync(ct);

        var pageCount = await baseQ.CountAsync(ct);

        var page = await baseQ
            .OrderByDescending(c => c.CreatedAt)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.CustomerCode,
                c.Status,
                c.CreatedAt,
                // Pick the wallet in the tenant's default currency; fall back to any wallet.
                Wallet = db.CustomerWallets
                    .Where(w => w.CustomerId == c.Id && w.CurrencyCode == defaultCurrency)
                    .Select(w => new { w.Balance, w.CurrencyCode })
                    .FirstOrDefault()
                    ?? db.CustomerWallets
                        .Where(w => w.CustomerId == c.Id)
                        .Select(w => new { w.Balance, w.CurrencyCode })
                        .FirstOrDefault(),
            })
            .ToListAsync(ct);

        var items = page.Select(x => new CustomerMasterViewModel
        {
            Id = x.Id,
            CustomerCode = x.CustomerCode,
            MobileMasked = "***",
            RunningWalletBalance = x.Wallet?.Balance ?? 0m,
            CurrencyCode = x.Wallet?.CurrencyCode ?? defaultCurrency ?? string.Empty,
            Status = x.Status,
            CreatedOn = x.CreatedAt.UtcDateTime,
        }).ToList();

        return new CustomerMasterList
        {
            customerMasterList = items,
            Total = totals?.Total ?? 0,
            Active = totals?.Active ?? 0,
            InActive = totals?.InActive ?? 0,
            PageSize = pageSize,
            PageNo = pageNo,
            PageCount = pageCount,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling(pageCount / (double)pageSize) : 0,
        };
    }

    /// <summary>Get by code.</summary>
    public async Task<CustomerMasterViewModel?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return null;
        }

        var defaultCurrency = await GetTenantDefaultCurrencyAsync(tenantId, ct);

        // IgnoreQueryFilters so soft-deleted (archived) customers remain
        // visible in the admin detail view — the global filter on DeletedAt
        // hides them from the main list, which is correct.
        var c = await db.Customers.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.CustomerCode == code, ct);
        if (c is null)
        {
            return null;
        }

        var wallet = await db.CustomerWallets.AsNoTracking()
            .Where(w => w.CustomerId == c.Id &&
                        (defaultCurrency == null || w.CurrencyCode == defaultCurrency))
            .OrderBy(w => w.CurrencyCode)
            .Select(w => new { w.Balance, w.CurrencyCode })
            .FirstOrDefaultAsync(ct);

        return new CustomerMasterViewModel
        {
            Id = c.Id,
            CustomerCode = c.CustomerCode,
            MobileMasked = "***",
            RunningWalletBalance = wallet?.Balance ?? 0m,
            CurrencyCode = wallet?.CurrencyCode ?? defaultCurrency ?? string.Empty,
            Status = c.Status,
            CreatedOn = c.CreatedAt.UtcDateTime,
        };
    }

    /// <summary>Add.</summary>
    public async Task<OperationResult> AddAsync(CustomerMasterViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (string.IsNullOrWhiteSpace(vm.Mobile))
        {
            return OperationResult.Fail("Mobile number is required.");
        }

        if (string.IsNullOrWhiteSpace(vm.WalletPin))
        {
            return OperationResult.Fail("Utility wallet PIN is required.");
        }

        var defaultCurrency = await GetTenantDefaultCurrencyAsync(tenantId, ct)
            ?? throw new InvalidOperationException("Tenant has no default currency configured.");

        var normalizedMobile = NormalizeMobile(vm.Mobile);
        var mobileHash = Sha256(normalizedMobile);

        var duplicate = await db.Customers.AsNoTracking()
            .AnyAsync(c => c.TenantId == tenantId && c.MobileLookupHash == mobileHash, ct);
        if (duplicate)
        {
            return OperationResult.Fail("A customer with this mobile already exists.");
        }

        var customerCode = string.IsNullOrWhiteSpace(vm.CustomerCode)
            ? $"C-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}"
            : vm.CustomerCode;

        var customer = new CustomerEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerCode = customerCode,
            GivenNameEnc = Encoding.UTF8.GetBytes("UNKNOWN"),   // TODO(GK-SEC-1): PII encryption stubbed - replace with pgcrypto pgp_sym_encrypt
            FamilyNameEnc = Encoding.UTF8.GetBytes("UNKNOWN"),
            MobileLookupHash = mobileHash,
            Status = "active",
        };
        db.Customers.Add(customer);

        // TODO(GK-SEC-1): primary mobile contact "encrypted" column is stubbed as plain UTF-8 bytes.
        db.CustomerContacts.Add(new CustomerContact
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Channel = "phone",
            ValueEnc = Encoding.UTF8.GetBytes(normalizedMobile),
            ValueLookupHash = mobileHash,
            IsPrimary = true,
        });

        // Utility wallet (default currency).
        db.CustomerWallets.Add(new CustomerWallet
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            CurrencyCode = defaultCurrency,
            Balance = 0m,
            PinHash = HashPin(vm.WalletPin!),
            Status = "active",
        });

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Edit.</summary>
    public async Task<OperationResult> EditAsync(CustomerMasterViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (string.IsNullOrWhiteSpace(vm.CustomerCode))
        {
            return OperationResult.Fail("Customer code is required.");
        }

        var customer = await db.Customers
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.CustomerCode == vm.CustomerCode, ct);
        if (customer is null)
        {
            return OperationResult.Fail($"Customer '{vm.CustomerCode}' not found.");
        }

        if (!string.IsNullOrWhiteSpace(vm.Mobile))
        {
            var normalizedMobile = NormalizeMobile(vm.Mobile);
            var mobileHash = Sha256(normalizedMobile);
            customer.MobileLookupHash = mobileHash;

            var primary = await db.CustomerContacts
                .Where(c => c.CustomerId == customer.Id && c.Channel == "phone" && c.IsPrimary)
                .FirstOrDefaultAsync(ct);
            if (primary is null)
            {
                db.CustomerContacts.Add(new CustomerContact
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    Channel = "phone",
                    ValueEnc = Encoding.UTF8.GetBytes(normalizedMobile),
                    ValueLookupHash = mobileHash,
                    IsPrimary = true,
                });
            }
            else
            {
                primary.ValueEnc = Encoding.UTF8.GetBytes(normalizedMobile);
                primary.ValueLookupHash = mobileHash;
            }
        }

        if (!string.IsNullOrWhiteSpace(vm.WalletPin))
        {
            var wallet = await db.CustomerWallets
                .Where(w => w.CustomerId == customer.Id)
                .OrderBy(w => w.CreatedAt)
                .FirstOrDefaultAsync(ct);
            wallet?.PinHash = HashPin(vm.WalletPin!);
        }

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Delete.</summary>
    public async Task<OperationResult> DeleteAsync(string code, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        var customer = await db.Customers
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.CustomerCode == code, ct);
        if (customer is null)
        {
            return OperationResult.Fail($"Customer '{code}' not found.");
        }

        customer.DeletedAt = DateTimeOffset.UtcNow;
        customer.Status = "archived";
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    // ─── helpers ───────────────────────────────────────────────────────────
    private async Task<string?> GetTenantDefaultCurrencyAsync(Guid tenantId, CancellationToken ct)
    {
        var row = await db.Database
            .SqlQueryRaw<string>(
                "SELECT default_currency_code AS \"Value\" FROM tenancy.tenant_configs WHERE tenant_id = {0}",
                tenantId)
            .FirstOrDefaultAsync(ct);
        return row;
    }

    private static string NormalizeMobile(string mobile)
    {
        // Trim, strip whitespace; lookup hash is computed over the normalised form.
        var sb = new StringBuilder(mobile.Length);
        foreach (var ch in mobile)
        {
            if (!char.IsWhiteSpace(ch))
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }

    private static byte[] Sha256(string s) => SHA256.HashData(Encoding.UTF8.GetBytes(s));

    private static string HashPin(string pin)
        => Convert.ToBase64String(Sha256(pin));     // TODO(GK-SEC-2): bare SHA-256 wallet-PIN hash - replace with Argon2id when the wallet-pin policy lands

    private static CustomerMasterList Empty(int pageSize, int pageNo) => new()
    {
        customerMasterList = new List<CustomerMasterViewModel>(),
        Total = 0,
        Active = 0,
        InActive = 0,
        PageSize = pageSize,
        PageNo = pageNo,
        PageCount = 0,
        TotalPage = 0,
    };
}
