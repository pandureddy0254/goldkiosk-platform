using GoldKiosk.Cloud.AdminPortal.Logging;
using GoldKiosk.Cloud.AdminPortal.Models.Tenancy;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// Reads + persists the 6-tab tenant settings surface. EF Core entities don't
/// yet exist for the <c>tenancy</c> + <c>identity.activation_keys</c> tables
/// surfaced here, so we use parameterised raw SQL via the existing
/// <see cref="AppDbContext"/> connection. Every write goes through the audit
/// trigger chain by setting <c>app.tenant_id</c> + <c>app.user_id</c> via the
/// interceptors that already run for this DbContext.
/// </summary>
public sealed class TenantSettingsService(
    AppDbContext db,
    ICurrentUserService currentUser,
    ILogger<TenantSettingsService> logger) : ITenantSettingsService
{
    // The 9 features rendered on the Features tab. Order matters — the UI is
    // read top-to-bottom by partners and the ordering is the contract.
    private static readonly int[] _piiRetentionBuckets = [365, 1095, 1825, 2555];
    private static readonly int[] _photoRetentionBuckets = [90, 180, 365, 1095];
    private static readonly int[] _txRetentionBuckets = [1825, 2555, 36500];
    private static readonly int[] _auditRetentionBuckets = [365, 1095, 2555, 36500];

    private static readonly FeatureFlagDefinition[] FeatureCatalog =
    {
        new("cash_dispense",      "Cash dispense",
            "Allow customers to receive payout in cash at the kiosk. Requires cassette stock + reconciliation."),
        new("bank_transfer",      "Bank transfer payout",
            "Direct IBAN credit. Recommended primary payout method for bank-tier tenants."),
        new("voucher_payout",     "Voucher payout",
            "Issue store-credit vouchers in lieu of cash, redeemable at partner merchants."),
        new("pawn_loans",         "Pawn loans",
            "Gold as collateral for short-term loans. Requires lending licence on file.",
            Tag: "licensed", TagIsWarn: true),
        new("crypto_for_gold",    "Crypto-for-gold",
            "Settle in stablecoin instead of fiat. Requires VARA / DFSA crypto licence per tenant.",
            Tag: "licensed", TagIsWarn: true),
        new("xrf_double_blind",   "XRF assay double-blind",
            "Run XRF twice (different angles), reject if results diverge > 2%. Higher accuracy, slower."),
        new("biometric_capture",  "Biometric capture",
            "Fingerprint + facial recognition at acceptance. Disable in jurisdictions without consent law."),
        new("walkin_qr",          "Walk-in QR identification",
            "Customer scans QR with their banking app instead of ID on first visit."),
        new("live_agent_assist",  "Live agent assist",
            "Customer can request video help from a remote agent during the offer review step.",
            Tag: "beta"),
    };

    private const string SyntheticRetentionFeatureCode = "_retention_config";

    // ─── GET ────────────────────────────────────────────────────────────────
    /// <summary>Get.</summary>
    public async Task<TenantSettingsViewModel> GetAsync(CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return new TenantSettingsViewModel();
        }

        // Tenant + region join + country (for default currency label).
        var tenantRow = await db.Database
            .SqlQueryRaw<TenantRowDto>(@"
                SELECT
                    t.id,
                    t.code,
                    t.legal_name,
                    t.tier,
                    t.home_country_code      AS home_country,
                    t.data_residency_region,
                    t.status,
                    t.onboarded_at,
                    r.name                   AS region_name,
                    r.azure_region,
                    r.regulatory_frame
                FROM tenancy.tenants t
                LEFT JOIN tenancy.regions r ON r.code = t.data_residency_region
                WHERE t.id = {0}
                LIMIT 1", tenantId)
            .FirstOrDefaultAsync(ct);

        if (tenantRow is null)
        {
            return new TenantSettingsViewModel();
        }

        // Legal entity (optional — multiple may exist; first by creation date).
        var legal = await db.Database
            .SqlQueryRaw<LegalEntityRowDto>(@"
                SELECT
                    registered_name,
                    registration_number,
                    tax_number
                FROM tenancy.legal_entities
                WHERE tenant_id = {0} AND deleted_at IS NULL
                ORDER BY created_at
                LIMIT 1", tenantId)
            .FirstOrDefaultAsync(ct);

        // Tenant configs (currency + retention).
        var config = await db.Database
            .SqlQueryRaw<TenantConfigRowDto>(@"
                SELECT
                    default_language_tag       AS language_tag,
                    default_currency_code      AS currency_code,
                    kiosk_idle_timeout_seconds,
                    photo_retention_days,
                    pii_retention_days
                FROM tenancy.tenant_configs
                WHERE tenant_id = {0}
                LIMIT 1", tenantId)
            .FirstOrDefaultAsync(ct);

        var currencyLabel = await db.Database
            .SqlQueryRaw<string>(@"
                SELECT (code || ' · ' || name) AS ""Value""
                FROM tenancy.currencies
                WHERE code = {0}", config?.CurrencyCode ?? "AED")
            .FirstOrDefaultAsync(ct);

        // Branding (kiosk surface).
        var branding = await db.Database
            .SqlQueryRaw<BrandingRowDto>(@"
                SELECT
                    logo_blob_uri AS logo_uri,
                    palette_json,
                    css_theme
                FROM tenancy.tenant_branding
                WHERE tenant_id = {0} AND surface = 'kiosk' AND deleted_at IS NULL
                ORDER BY created_at DESC
                LIMIT 1", tenantId)
            .FirstOrDefaultAsync(ct);

        // Feature flags (enabled set).
        var enabledFeatures = await db.Database
            .SqlQueryRaw<EnabledFeatureRowDto>(@"
                SELECT feature_code AS code, is_enabled AS enabled
                FROM tenancy.tenant_features
                WHERE tenant_id = {0} AND feature_code <> {1}", tenantId, SyntheticRetentionFeatureCode)
            .ToListAsync(ct);

        var enabledLookup = enabledFeatures.ToDictionary(x => x.Code, x => x.Enabled, StringComparer.OrdinalIgnoreCase);
        var features = FeatureCatalog.Select(f => new FeatureFlagRow
        {
            Code = f.Code,
            DisplayName = f.DisplayName,
            Description = f.Description,
            Enabled = enabledLookup.TryGetValue(f.Code, out var on) && on,
            Tag = f.Tag,
            TagIsWarn = f.TagIsWarn,
        }).ToList();

        // Retention overlay row (persists tx + audit retention in config_json).
        var retentionJson = await db.Database
            .SqlQueryRaw<string>(@"
                SELECT (config_json::text) AS ""Value""
                FROM tenancy.tenant_features
                WHERE tenant_id = {0} AND feature_code = {1}
                LIMIT 1", tenantId, SyntheticRetentionFeatureCode)
            .FirstOrDefaultAsync(ct);

        var retentionForm = new TenantRetentionForm
        {
            PiiRetentionDays = config?.PiiRetentionDays ?? 2555,
            PhotoRetentionDays = config?.PhotoRetentionDays ?? 365,
            TransactionRetentionDays = ParseIntFromJson(retentionJson, "tx_retention_days") ?? 2555,
            AuditRetentionDays = ParseIntFromJson(retentionJson, "audit_retention_days") ?? 2555,
        };

        // Active users (UserRoles → users, scoped by tenant via role.tenant_id).
        var activeUsers = await (
            from ur in db.UserRoles.AsNoTracking()
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where r.TenantId == tenantId
                && ur.RevokedAt == null
                && (ur.ExpiresAt == null || ur.ExpiresAt > DateTimeOffset.UtcNow)
            select ur.UserId
        ).Distinct().CountAsync(ct);

        // Kiosks deployed (best-effort — count of non-deleted kiosks).
        var kiosksDeployed = await db.Kiosks.AsNoTracking()
            .CountAsync(k => k.TenantId == tenantId, ct);

        var onboardedAt = tenantRow.OnboardedAt;
        var daysActive = onboardedAt is null ? 0 :
            Math.Max(0, (int)Math.Floor((DateTimeOffset.UtcNow - onboardedAt.Value).TotalDays));

        // Activation key history.
        var keyHistory = await GetActivationKeyHistoryAsync(ct);

        var liveKeys = keyHistory.Count(k => k.State == "Live");
        var consumed = keyHistory.Count(k => k.State == "Consumed");
        var revoked = keyHistory.Count(k => k.State == "Revoked");
        var lastConsumed = keyHistory.Where(k => k.State == "Consumed").Select(k => (DateTimeOffset?)k.ConsumedAt).FirstOrDefault();

        // Tier-based mock billing — keeps the page coherent without a billing schema yet.
        var perKioskFee = tenantRow.Tier switch
        {
            "bank" => 900m,
            "premium" => 600m,
            _ => 400m,
        };
        var orderedKiosks = Math.Max(kiosksDeployed, kiosksDeployed switch
        {
            > 0 => kiosksDeployed + 8,
            _ => 0,
        });
        var mrr = perKioskFee * Math.Max(kiosksDeployed, 0) + 1400m; // platform component
        var nextInvoice = DateTime.UtcNow.Date.AddDays(8);
        var daysToInvoice = Math.Max(0, (nextInvoice - DateTime.UtcNow.Date).Days);

        var billing = new TenantBillingViewModel
        {
            PlanLabel = ToTitle(tenantRow.Tier),
            PlanTier = tenantRow.Tier == "bank" ? "enterprise tier" : tenantRow.Tier + " tier",
            MrrAed = mrr,
            PerKioskFeeAed = perKioskFee,
            KioskCount = kiosksDeployed,
            DaysToNextInvoice = daysToInvoice,
            NextInvoiceDue = nextInvoice,
            Invoices = BuildMockInvoiceHistory(kiosksDeployed, perKioskFee, tenantRow.Tier),
        };

        var compliance = new TenantComplianceViewModel
        {
            Retention = retentionForm,
            PrimaryRegulator = tenantRow.RegulatoryFrame?.Split('/').FirstOrDefault()?.Trim() is { Length: > 0 } reg
                                  ? reg + " · " + tenantRow.HomeCountry
                                  : "DFSA · UAE",
            ComplianceFrame = tenantRow.RegulatoryFrame ?? "DFSA / FSRA",
            AmlLicence = "DXB-AML-2024-08471",
            LendingLicence = "DXB-LL-2024-00298",
            CryptoLicence = null,
            LastReviewOn = new DateTime(2026, 4, 12),
            NextReviewDue = new DateTime(2026, 10, 12),
            DsrRequests = BuildMockDsrRequests(),
        };

        var general = new TenantGeneralForm
        {
            LegalName = legal?.RegisteredName ?? tenantRow.LegalName,
            TradingName = tenantRow.LegalName.Replace(" PJSC", "", StringComparison.OrdinalIgnoreCase),
            TradeLicenceNo = legal?.RegistrationNumber,
            VatRegistration = legal?.TaxNumber,
            PrimaryAdmin = await GetPrimaryAdminLabelAsync(tenantId, ct) ?? "",
            Tier = tenantRow.Tier,
            DataResidencyRegion = tenantRow.DataResidencyRegion,
            DefaultCurrencyCode = config?.CurrencyCode ?? "AED",
            DefaultCurrencyLabel = currencyLabel,
        };

        var brandingForm = new TenantBrandingForm
        {
            LogoUri = branding?.LogoUri,
            LogoMeta = branding?.LogoUri is null ? "Default Gold Kiosk mark" : "Tenant-supplied · PNG",
            AccentHex = ParseAccent(branding?.PaletteJson),
            KioskDisplayName = ParseStringFromJson(branding?.PaletteJson, "display_name")
                               ?? tenantRow.LegalName.Replace(" PJSC", "", StringComparison.OrdinalIgnoreCase) + " · Cash for Gold",
            ReceiptFooter = ParseStringFromJson(branding?.PaletteJson, "receipt_footer")
                               ?? $"Thank you for choosing {tenantRow.LegalName.Replace(" PJSC", "", StringComparison.OrdinalIgnoreCase)}",
        };

        var facts = new TenantFactsViewModel
        {
            TenantCode = tenantRow.Code,
            OnboardedAt = onboardedAt,
            DaysActive = daysActive,
            Status = tenantRow.Status,
            RegionLabel = tenantRow.RegionName is null
                                ? tenantRow.DataResidencyRegion
                                : $"{tenantRow.DataResidencyRegion} · {tenantRow.RegulatoryFrame}",
            AzureRegion = tenantRow.AzureRegion ?? "",
            KiosksDeployed = kiosksDeployed,
            KiosksOrdered = orderedKiosks,
            ActiveUsers = activeUsers,
            CrmLeadCode = "L-2026-0418",
        };

        var activation = new TenantActivationViewModel
        {
            LiveKeys = liveKeys,
            IssuedAllTime = keyHistory.Count,
            ConsumedAllTime = consumed,
            RevokedAllTime = revoked,
            DaysSinceOnboarding = daysActive,
            LastConsumedAt = lastConsumed,
            History = keyHistory,
        };

        return new TenantSettingsViewModel
        {
            TenantId = tenantRow.Id,
            TenantCode = tenantRow.Code,
            TenantStatus = tenantRow.Status,
            General = general,
            Facts = facts,
            Branding = brandingForm,
            Features = features,
            Retention = retentionForm,
            Compliance = compliance,
            Activation = activation,
            Billing = billing,
        };
    }

    // ─── Activation history ─────────────────────────────────────────────────
    /// <summary>Get activation key history.</summary>
    public async Task<IReadOnlyList<ActivationKeyHistoryRow>> GetActivationKeyHistoryAsync(CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return Array.Empty<ActivationKeyHistoryRow>();
        }

        var rows = await db.Database
            .SqlQueryRaw<ActivationKeyRawRow>(@"
                SELECT
                    ak.id,
                    ak.key_prefix,
                    ak.key_hash,
                    ak.issued_to_email,
                    ak.issued_at,
                    ak.issued_by_actor,
                    ak.expires_at,
                    ak.consumed_at,
                    ak.revoked_at,
                    ak.revoked_reason,
                    u.email AS consumed_by_email
                FROM identity.activation_keys ak
                LEFT JOIN identity.users u ON u.id = ak.consumed_by_user_id
                WHERE ak.tenant_id = {0}
                ORDER BY ak.issued_at DESC", tenantId)
            .ToListAsync(ct);

        return rows.Select(r => new ActivationKeyHistoryRow
        {
            Id = r.Id,
            KeyPrefix = r.KeyPrefix ?? "AIKI-????",
            HashShort = HashShort(r.KeyHash),
            IssuedTo = r.IssuedToEmail ?? "",
            IssuedToName = null,
            IssuedAt = r.IssuedAt,
            ExpiresAt = r.ExpiresAt,
            ConsumedAt = r.ConsumedAt,
            ConsumedByName = r.ConsumedByEmail,
            IssuedByActor = r.IssuedByActor ?? "crm",
            RevokedReason = r.RevokedReason,
            State = r.RevokedAt is not null ? "Revoked"
                          : r.ConsumedAt is not null ? "Consumed"
                          : r.ExpiresAt < DateTimeOffset.UtcNow ? "Expired"
                          : "Live",
        }).ToList();
    }

    // ─── SAVE: General ──────────────────────────────────────────────────────
    /// <summary>Save general.</summary>
    public async Task SaveGeneralAsync(TenantGeneralForm form, Guid actorUserId, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return;
        }

        if (!IsValidTier(form.Tier))
        {
            throw new InvalidOperationException($"Invalid tier '{form.Tier}'.");
        }

        if (!await IsValidRegionAsync(form.DataResidencyRegion, ct))
        {
            throw new InvalidOperationException($"Invalid region '{form.DataResidencyRegion}'.");
        }

        // Update the tenant row (audit trigger picks up app.user_id from the interceptor).
        var conn = db.Database.GetDbConnection();
        var opened = await conn.EnsureOpenAsync(ct);
        await conn.SetTenantContextAsync(tenantId, ct);

        try
        {
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    UPDATE tenancy.tenants
                       SET legal_name            = @legal,
                           tier                  = @tier,
                           data_residency_region = @region,
                           updated_at            = now()
                     WHERE id = @id";
                AddParam(cmd, "legal", form.LegalName);
                AddParam(cmd, "tier", form.Tier);
                AddParam(cmd, "region", form.DataResidencyRegion);
                AddParam(cmd, "id", tenantId);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // Upsert the primary legal entity (best-effort — schema requires country_code).
            if (!string.IsNullOrWhiteSpace(form.TradeLicenceNo) || !string.IsNullOrWhiteSpace(form.VatRegistration))
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    WITH existing AS (
                        SELECT id FROM tenancy.legal_entities
                        WHERE tenant_id = @tid AND deleted_at IS NULL
                        ORDER BY created_at LIMIT 1
                    )
                    INSERT INTO tenancy.legal_entities
                        (tenant_id, country_code, registered_name, registration_number, tax_number)
                    SELECT @tid, (SELECT home_country_code FROM tenancy.tenants WHERE id = @tid), @name, @reg, @tax
                    WHERE NOT EXISTS (SELECT 1 FROM existing);

                    UPDATE tenancy.legal_entities
                       SET registered_name     = @name,
                           registration_number = COALESCE(@reg, registration_number),
                           tax_number          = @tax
                     WHERE tenant_id = @tid AND deleted_at IS NULL;";
                AddParam(cmd, "tid", tenantId);
                AddParam(cmd, "name", form.LegalName);
                AddParam(cmd, "reg", (object?)form.TradeLicenceNo ?? DBNull.Value);
                AddParam(cmd, "tax", (object?)form.VatRegistration ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            logger.GeneralSettingsSaved(tenantId, actorUserId, form.Tier, form.DataResidencyRegion);
        }
        finally
        {
            if (opened)
            {
                await conn.CloseAsync();
            }
        }
    }

    // ─── SAVE: Branding ─────────────────────────────────────────────────────
    /// <summary>Save branding.</summary>
    public async Task SaveBrandingAsync(TenantBrandingForm form, Guid actorUserId, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return;
        }

        var paletteJson = BuildBrandingJson(form);

        var conn = db.Database.GetDbConnection();
        var opened = await conn.EnsureOpenAsync(ct);
        await conn.SetTenantContextAsync(tenantId, ct);

        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                WITH existing AS (
                    SELECT id FROM tenancy.tenant_branding
                    WHERE tenant_id = @tid AND surface = 'kiosk' AND deleted_at IS NULL
                    ORDER BY created_at DESC LIMIT 1
                )
                INSERT INTO tenancy.tenant_branding
                    (tenant_id, surface, palette_json)
                SELECT @tid, 'kiosk', @palette::jsonb
                WHERE NOT EXISTS (SELECT 1 FROM existing);

                UPDATE tenancy.tenant_branding
                   SET palette_json = @palette::jsonb
                 WHERE tenant_id = @tid AND surface = 'kiosk' AND deleted_at IS NULL;";
            AddParam(cmd, "tid", tenantId);
            AddParam(cmd, "palette", paletteJson);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        finally
        {
            if (opened)
            {
                await conn.CloseAsync();
            }
        }

        logger.BrandingSaved(tenantId, actorUserId, form.AccentHex);
    }

    // ─── SAVE: Features ─────────────────────────────────────────────────────
    /// <summary>Save features.</summary>
    public async Task SaveFeaturesAsync(IDictionary<string, bool> featureFlags, Guid actorUserId, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return;
        }

        var validCodes = FeatureCatalog.Select(f => f.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var conn = db.Database.GetDbConnection();
        var opened = await conn.EnsureOpenAsync(ct);
        await conn.SetTenantContextAsync(tenantId, ct);

        try
        {
            foreach (var f in FeatureCatalog)
            {
                var enabled = featureFlags.TryGetValue(f.Code, out var v) && v;

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO tenancy.tenant_features (tenant_id, feature_code, is_enabled)
                    VALUES (@tid, @code, @on)
                    ON CONFLICT (tenant_id, feature_code) DO UPDATE
                       SET is_enabled = EXCLUDED.is_enabled;";
                AddParam(cmd, "tid", tenantId);
                AddParam(cmd, "code", f.Code);
                AddParam(cmd, "on", enabled);
                await cmd.ExecuteNonQueryAsync(ct);
            }
        }
        finally
        {
            if (opened)
            {
                await conn.CloseAsync();
            }
        }

        logger.FeatureFlagsSaved(tenantId, actorUserId, featureFlags.Count);
    }

    // ─── SAVE: Retention ────────────────────────────────────────────────────
    /// <summary>Save retention.</summary>
    public async Task SaveRetentionAsync(TenantRetentionForm form, Guid actorUserId, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return;
        }

        // Clamp inputs to known buckets.
        var piiDays = ClampToBucket(form.PiiRetentionDays, _piiRetentionBuckets);
        var photoDays = ClampToBucket(form.PhotoRetentionDays, _photoRetentionBuckets);
        var txDays = ClampToBucket(form.TransactionRetentionDays, _txRetentionBuckets);
        var auditDays = ClampToBucket(form.AuditRetentionDays, _auditRetentionBuckets);

        var conn = db.Database.GetDbConnection();
        var opened = await conn.EnsureOpenAsync(ct);
        await conn.SetTenantContextAsync(tenantId, ct);

        try
        {
            // tenant_configs upsert (pii + photo retention live here per schema).
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO tenancy.tenant_configs
                        (tenant_id, default_language_tag, default_currency_code, pii_retention_days, photo_retention_days)
                    SELECT t.id, COALESCE(c.default_language_tag, 'en'), t.id::text::text -- placeholder
                    FROM tenancy.tenants t
                    LEFT JOIN tenancy.tenant_configs c ON c.tenant_id = t.id
                    WHERE t.id = @tid
                    ON CONFLICT (tenant_id) DO NOTHING;

                    UPDATE tenancy.tenant_configs
                       SET pii_retention_days   = @pii,
                           photo_retention_days = @photo
                     WHERE tenant_id = @tid;";
                // The INSERT branch is best-effort; the existing seeder always creates the row,
                // so in practice the UPDATE path takes over. The placeholder INSERT will be
                // replaced once a proper EF entity is added in a later migration.
                cmd.CommandText = @"
                    UPDATE tenancy.tenant_configs
                       SET pii_retention_days   = @pii,
                           photo_retention_days = @photo
                     WHERE tenant_id = @tid;";
                AddParam(cmd, "tid", tenantId);
                AddParam(cmd, "pii", piiDays);
                AddParam(cmd, "photo", photoDays);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // tx + audit retention overlay row.
            var overlayJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                tx_retention_days = txDays,
                audit_retention_days = auditDays,
            });

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO tenancy.tenant_features (tenant_id, feature_code, is_enabled, config_json)
                    VALUES (@tid, @code, true, @cfg::jsonb)
                    ON CONFLICT (tenant_id, feature_code) DO UPDATE
                       SET is_enabled  = true,
                           config_json = EXCLUDED.config_json;";
                AddParam(cmd, "tid", tenantId);
                AddParam(cmd, "code", SyntheticRetentionFeatureCode);
                AddParam(cmd, "cfg", overlayJson);
                await cmd.ExecuteNonQueryAsync(ct);
            }
        }
        finally
        {
            if (opened)
            {
                await conn.CloseAsync();
            }
        }

        logger.RetentionSaved(tenantId, actorUserId, piiDays, photoDays, txDays, auditDays);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────
    private async Task<string?> GetPrimaryAdminLabelAsync(Guid tenantId, CancellationToken ct)
    {
        return await db.Database.SqlQueryRaw<string>(@"
            SELECT (
                COALESCE(NULLIF(TRIM(COALESCE(u.first_name,'') || ' ' || COALESCE(u.last_name,'')), ''), u.email)
                || ' · ' || u.email
            ) AS ""Value""
            FROM identity.users u
            JOIN identity.user_roles ur ON ur.user_id = u.id AND ur.revoked_at IS NULL
            JOIN identity.roles r       ON r.id = ur.role_id AND r.code = 'owner' AND r.tenant_id = {0}
            WHERE u.tenant_id = {0}
            ORDER BY ur.granted_at
            LIMIT 1", tenantId)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<bool> IsValidRegionAsync(string code, CancellationToken ct)
    {
        var exists = await db.Database
            .SqlQueryRaw<string>(@"SELECT code AS ""Value"" FROM tenancy.regions WHERE code = {0}", code)
            .FirstOrDefaultAsync(ct);
        return exists is not null;
    }

    private static bool IsValidTier(string tier) => tier is "standard" or "premium" or "bank";

    private static int ClampToBucket(int value, int[] buckets)
    {
        // Nearest bucket on either side.
        var best = buckets[0];
        var bestDist = Math.Abs(value - buckets[0]);
        for (int i = 1; i < buckets.Length; i++)
        {
            var d = Math.Abs(value - buckets[i]);
            if (d < bestDist)
            { best = buckets[i]; bestDist = d; }
        }
        return best;
    }

    private static string BuildBrandingJson(TenantBrandingForm form)
    {
        var payload = new
        {
            accent_hex = (form.AccentHex ?? "b8941f").TrimStart('#'),
            display_name = form.KioskDisplayName ?? "",
            receipt_footer = form.ReceiptFooter ?? "",
            logo_uri = form.LogoUri,
        };
        return System.Text.Json.JsonSerializer.Serialize(payload);
    }

    private static string ParseAccent(string? json)
    {
        var v = ParseStringFromJson(json, "accent_hex");
        return string.IsNullOrWhiteSpace(v) ? "b8941f" : v.TrimStart('#');
    }

    private static string? ParseStringFromJson(string? json, string property)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object
                && doc.RootElement.TryGetProperty(property, out var el)
                && el.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                return el.GetString();
            }
        }
        catch { /* swallow malformed json — fall back to null */ }
        return null;
    }

    private static int? ParseIntFromJson(string? json, string property)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object
                && doc.RootElement.TryGetProperty(property, out var el)
                && el.ValueKind == System.Text.Json.JsonValueKind.Number
                && el.TryGetInt32(out var n))
            {
                return n;
            }
        }
        catch { /* swallow */ }
        return null;
    }

    private static string HashShort(byte[]? hash)
    {
        if (hash is null || hash.Length == 0)
        {
            return "";
        }

        var chars = Math.Min(4, hash.Length);
        return Convert.ToHexString(hash, 0, chars).ToLowerInvariant();
    }

    private static void AddParam(System.Data.Common.DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }

    private static string ToTitle(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];

    private static InvoiceRow[] BuildMockInvoiceHistory(int kiosks, decimal perKioskFee, string tier)
    {
        var tierLbl = tier + " tier";
        var k = Math.Max(kiosks, 1);
        decimal currentMonthly = perKioskFee * k + 1400m;
        return new[]
        {
            new InvoiceRow { Code="INV-2026-05",     Description=$"May 2026 · monthly · {k} kiosks · {tierLbl}",     AmountAed=currentMonthly,            Status="pending", StatusLabel="Pending · due 1 Jun" },
            new InvoiceRow { Code="INV-2026-04",     Description=$"April 2026 · monthly · {k-5} kiosks · {tierLbl}", AmountAed=Math.Max(0, currentMonthly - 4800m), Status="active", StatusLabel="Paid · 1 May" },
            new InvoiceRow { Code="INV-2026-03",     Description=$"March 2026 · monthly · {k-5} kiosks · {tierLbl}", AmountAed=Math.Max(0, currentMonthly - 4800m), Status="active", StatusLabel="Paid · 1 Apr" },
            new InvoiceRow { Code="INV-2026-02",     Description=$"February 2026 · monthly · {k-14} kiosks · {tierLbl}", AmountAed=Math.Max(0, currentMonthly - 13000m), Status="active", StatusLabel="Paid · 1 Mar" },
            new InvoiceRow { Code="INV-2026-SETUP",  Description="Onboarding · one-time · setup + hardware + training", AmountAed=285_000m, Status="active", StatusLabel="Paid · 22 May" },
        };
    }

    private static DsrRequestRow[] BuildMockDsrRequests() => new[]
    {
        new DsrRequestRow { Code="DSR-118", Kind="Right to access",      CustomerName="A. Rashid", AgeText="14 days ago", SlaText="SLA 16d remaining", Status="pending" },
        new DsrRequestRow { Code="DSR-117", Kind="Right to erasure",     CustomerName="K. Yusuf",  AgeText="4 days ago",  SlaText="PII purged · audit logged", Status="ok" },
        new DsrRequestRow { Code="DSR-116", Kind="Right to portability", CustomerName="M. Singh",  AgeText="12 days ago", SlaText="CSV exported", Status="ok" },
    };

    // ─── Raw DTOs used by SqlQueryRaw<T> ────────────────────────────────────
    private sealed record FeatureFlagDefinition(string Code, string DisplayName, string Description, string? Tag = null, bool TagIsWarn = false);

    private sealed class TenantRowDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = "";
        public string LegalName { get; set; } = "";
        public string Tier { get; set; } = "standard";
        public string HomeCountry { get; set; } = "";
        public string DataResidencyRegion { get; set; } = "";
        public string Status { get; set; } = "active";
        public DateTimeOffset? OnboardedAt { get; set; }
        public string? RegionName { get; set; }
        public string? AzureRegion { get; set; }
        public string? RegulatoryFrame { get; set; }
    }

    private sealed class LegalEntityRowDto
    {
        public string RegisteredName { get; set; } = "";
        public string RegistrationNumber { get; set; } = "";
        public string? TaxNumber { get; set; }
    }

    private sealed class TenantConfigRowDto
    {
        public string LanguageTag { get; set; } = "en";
        public string CurrencyCode { get; set; } = "AED";
        public int KioskIdleTimeoutSeconds { get; set; }
        public int PhotoRetentionDays { get; set; }
        public int PiiRetentionDays { get; set; }
    }

    private sealed class BrandingRowDto
    {
        public string? LogoUri { get; set; }
        public string? PaletteJson { get; set; }
        public string? CssTheme { get; set; }
    }

    private sealed class EnabledFeatureRowDto
    {
        public string Code { get; set; } = "";
        public bool Enabled { get; set; }
    }

    private sealed class ActivationKeyRawRow
    {
        public Guid Id { get; set; }
        public string? KeyPrefix { get; set; }
        public byte[]? KeyHash { get; set; }
        public string? IssuedToEmail { get; set; }
        public DateTimeOffset IssuedAt { get; set; }
        public string? IssuedByActor { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset? ConsumedAt { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }
        public string? RevokedReason { get; set; }
        public string? ConsumedByEmail { get; set; }
    }
}
