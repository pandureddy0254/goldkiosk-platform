using System.Security.Cryptography;
using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Entities.Kiosk;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Kiosk security token service.</summary>
public sealed class KioskSecurityTokenService(AppDbContext db, ICurrentUserService currentUser) : IKioskSecurityTokenService
{
    private static readonly TimeSpan _defaultValidity = TimeSpan.FromDays(90);

    /// <summary>List.</summary>
    public async Task<KioskSecurityTokenList> ListAsync(Guid? kioskId, int pageSize, int pageNo, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return EmptyResult(pageSize, pageNo);
        }

        var q =
            from tok in db.KioskSecurityTokens.AsNoTracking()
            join k in db.Kiosks.AsNoTracking() on tok.KioskId equals k.Id
            where k.TenantId == tenantId
            select new { tok, k.Code };

        if (kioskId is { } kid)
        {
            q = q.Where(x => x.tok.KioskId == kid);
        }

        var totals = await q.GroupBy(_ => 1).Select(g => new
        {
            Total = g.Count(),
            Active = g.Count(x => x.tok.RevokedAt == null),
            Revoked = g.Count(x => x.tok.RevokedAt != null),
        }).FirstOrDefaultAsync(ct);

        var totalFiltered = await q.CountAsync(ct);

        // Two-step projection: shaper cannot coerce DateTimeOffset -> Nullable<DateTime> inside a JOIN shape.
        var raw = await q
            .OrderByDescending(x => x.tok.IssuedAt)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .Select(x => new
            {
                x.tok.Id,
                x.tok.KioskId,
                KioskCode = x.Code,
                x.tok.IssuedAt,
                x.tok.ExpiresAt,
                x.tok.RevokedAt,
                x.tok.LastUsedAt,
            })
            .ToListAsync(ct);

        var items = raw.Select(x => new KioskSecurityTokenViewModel
        {
            Id = x.Id,
            KioskId = x.KioskId,
            KioskCode = x.KioskCode,
            TokenHashPreview = "",                                    // populated below
            IssuedAt = x.IssuedAt.UtcDateTime,
            ExpiresAt = x.ExpiresAt.UtcDateTime,
            RevokedAt = x.RevokedAt?.UtcDateTime,
            LastUsedAt = x.LastUsedAt?.UtcDateTime,
        }).ToList();

        // Hash preview (first 8 hex chars) — pull hashes in a second pass to avoid pulling bytea over LINQ projection.
        var ids = items.Select(i => i.Id).ToList();
        var hashes = await db.KioskSecurityTokens.AsNoTracking()
            .Where(t => ids.Contains(t.Id))
            .Select(t => new { t.Id, t.TokenHash })
            .ToListAsync(ct);
        var map = hashes.ToDictionary(x => x.Id, x => x.TokenHash);
        foreach (var it in items)
        {
            if (map.TryGetValue(it.Id, out var h) && h is { Length: > 0 })
            {
                it.TokenHashPreview = Convert.ToHexString(h, 0, Math.Min(4, h.Length));
            }
        }

        return new KioskSecurityTokenList
        {
            Items = items,
            Total = totals?.Total ?? 0,
            Active = totals?.Active ?? 0,
            Revoked = totals?.Revoked ?? 0,
            PageSize = pageSize,
            PageNo = pageNo,
            PageCount = totalFiltered,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling(totalFiltered / (double)pageSize) : 0,
        };
    }

    /// <summary>Issue.</summary>
    public async Task<KioskSecurityTokenList> IssueAsync(Guid kioskId, TimeSpan? validity, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return EmptyResult(10, 1);
        }

        // Confirm kiosk belongs to caller's tenant.
        var kioskOk = await db.Kiosks.AnyAsync(k => k.Id == kioskId && k.TenantId == tenantId, ct);
        if (!kioskOk)
        {
            return EmptyResult(10, 1);
        }

        // gen_random_uuid() returns a uuid; use it as the raw token material as required by the brief.
        var rawUuid = await db.Database
            .SqlQueryRaw<Guid>("SELECT gen_random_uuid() AS \"Value\"")
            .FirstAsync(ct);

        // Raw token: hex-encode the uuid bytes (32 chars). Surfaced ONCE to the caller.
        var rawBytes = rawUuid.ToByteArray();
        var rawToken = Convert.ToHexString(rawBytes);
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken));

        var entity = new KioskSecurityToken
        {
            Id = Guid.NewGuid(),
            KioskId = kioskId,
            TokenHash = hash,
            IssuedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(validity ?? _defaultValidity),
            IssuedByUserId = currentUser.UserId,
        };

        db.KioskSecurityTokens.Add(entity);
        await db.SaveChangesAsync(ct);

        var list = await ListAsync(kioskId, 10, 1, ct);
        list.NewlyIssuedRawToken = rawToken;
        list.NewlyIssuedKioskId = kioskId;
        return list;
    }

    /// <summary>Revoke.</summary>
    public async Task<OperationResult> RevokeAsync(Guid tokenId, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        var tok = await (from t in db.KioskSecurityTokens
                         join k in db.Kiosks on t.KioskId equals k.Id
                         where t.Id == tokenId && k.TenantId == tenantId
                         select t).FirstOrDefaultAsync(ct);
        if (tok is null)
        {
            return OperationResult.Fail("Token not found.");
        }

        if (tok.RevokedAt is not null)
        {
            return OperationResult.Ok();
        }

        tok.RevokedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    private static KioskSecurityTokenList EmptyResult(int pageSize, int pageNo) => new()
    {
        Items = new List<KioskSecurityTokenViewModel>(),
        PageSize = pageSize,
        PageNo = pageNo,
    };
}
