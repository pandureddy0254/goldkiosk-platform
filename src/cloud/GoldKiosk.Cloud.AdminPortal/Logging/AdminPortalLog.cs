namespace GoldKiosk.Cloud.AdminPortal.Logging;

/// <summary>
/// Source-generated log messages for the Admin Portal (LoggerMessage delegates per the
/// coding standard, mirroring <c>KioskApiLog</c> in Kiosk.Api). Messages carry
/// identifiers only — never emails, keys, passwords or other PII.
/// </summary>
internal static partial class AdminPortalLog
{
    // ── Account / sign-in (2000–2019) ───────────────────────────────────────

    /// <summary>Account lockout during sign-in.</summary>
    [LoggerMessage(EventId = 2000, Level = LogLevel.Warning,
        Message = "Account {UserId} is locked out.")]
    public static partial void AccountLockedOut(this ILogger logger, Guid? userId);

    /// <summary>Successful sign-in.</summary>
    [LoggerMessage(EventId = 2001, Level = LogLevel.Information,
        Message = "User {UserId} signed in.")]
    public static partial void UserSignedIn(this ILogger logger, Guid? userId);

    /// <summary>Activation key rejected by the database function.</summary>
    [LoggerMessage(EventId = 2002, Level = LogLevel.Warning,
        Message = "Activation key validation failed (key/email omitted from logs per PII policy).")]
    public static partial void ActivationKeyValidationFailed(this ILogger logger, Exception exception);

    /// <summary>Activation-key DB objects (migration 0033) are missing.</summary>
    [LoggerMessage(EventId = 2003, Level = LogLevel.Error,
        Message = "Activation key DB objects missing. Migration 0033 not applied?")]
    public static partial void ActivationKeyDbObjectsMissing(this ILogger logger, Exception exception);

    /// <summary>Activation key consumed by a concurrent request.</summary>
    [LoggerMessage(EventId = 2004, Level = LogLevel.Warning,
        Message = "Activation key consumed concurrently - rolling back user {UserId}")]
    public static partial void ActivationKeyConsumedConcurrently(this ILogger logger, Exception exception, Guid? userId);

    /// <summary>Tenant activated with its first owner user.</summary>
    [LoggerMessage(EventId = 2005, Level = LogLevel.Information,
        Message = "Activated tenant {TenantId} with new owner {UserId}.")]
    public static partial void TenantActivated(this ILogger logger, Guid? tenantId, Guid? userId);

    /// <summary>Invitation accepted (invitee email intentionally omitted).</summary>
    [LoggerMessage(EventId = 2006, Level = LogLevel.Information,
        Message = "Invitation {InviteId} accepted by user {UserId} into tenant {TenantId} as {Role}.")]
    public static partial void InvitationAccepted(this ILogger logger, Guid? inviteId, Guid? userId, Guid? tenantId, string? role);

    // ── Partner API credentials (2020–2039) ─────────────────────────────────

    /// <summary>Flash payload for a just-created credential failed to deserialize.</summary>
    [LoggerMessage(EventId = 2020, Level = LogLevel.Warning,
        Message = "Failed to deserialize just-created credential flash payload.")]
    public static partial void CredentialFlashDeserializeFailed(this ILogger logger, Exception exception);

    /// <summary>Create request for a partner API credential failed validation.</summary>
    [LoggerMessage(EventId = 2021, Level = LogLevel.Warning,
        Message = "Invalid Create request for partner API credential.")]
    public static partial void CredentialCreateRequestInvalid(this ILogger logger, Exception exception);

    /// <summary>Credential generation failed.</summary>
    [LoggerMessage(EventId = 2022, Level = LogLevel.Error,
        Message = "Failed to generate partner API credential.")]
    public static partial void CredentialGenerateFailed(this ILogger logger, Exception exception);

    /// <summary>Credential revocation failed.</summary>
    [LoggerMessage(EventId = 2023, Level = LogLevel.Error,
        Message = "Failed to revoke partner API credential. Id={Id}")]
    public static partial void CredentialRevokeFailed(this ILogger logger, Exception exception, Guid? id);

    /// <summary>Partner API credential created.</summary>
    [LoggerMessage(EventId = 2024, Level = LogLevel.Information,
        Message = "Partner API credential created. Id={Id} AppId={AppId} Env={Env} Tenant={Tenant} Actor={Actor}")]
    public static partial void PartnerCredentialCreated(this ILogger logger, Guid? id, string? appId, string? env, Guid? tenant, Guid? actor);

    /// <summary>Partner API credential revoke attempted.</summary>
    [LoggerMessage(EventId = 2025, Level = LogLevel.Information,
        Message = "Partner API credential revoke attempted. Id={Id} RowsAffected={Rows} Reason={Reason} Actor={Actor}")]
    public static partial void PartnerCredentialRevokeAttempted(this ILogger logger, Guid? id, int? rows, string? reason, Guid? actor);

    // ── Management actions (2040–2069) ──────────────────────────────────────

    /// <summary>AddCustomer write failed.</summary>
    [LoggerMessage(EventId = 2040, Level = LogLevel.Warning,
        Message = "AddCustomer failed: {Errors}")]
    public static partial void AddCustomerFailed(this ILogger logger, string? errors);

    /// <summary>Audited PII unmask action.</summary>
    [LoggerMessage(EventId = 2041, Level = LogLevel.Information,
        Message = "PII unmask for customer {CustomerId}, field {Field}, actor {ActorId}, reason: {Reason}")]
    public static partial void PiiUnmasked(this ILogger logger, Guid? customerId, string? field, Guid? actorId, string? reason);

    /// <summary>Feedback mark-as-read failed.</summary>
    [LoggerMessage(EventId = 2042, Level = LogLevel.Warning,
        Message = "MarkAsRead failed: {Errors}")]
    public static partial void MarkAsReadFailed(this ILogger logger, string? errors);

    /// <summary>Feedback mark-as-unread failed.</summary>
    [LoggerMessage(EventId = 2043, Level = LogLevel.Warning,
        Message = "MarkAsUnread failed: {Errors}")]
    public static partial void MarkAsUnreadFailed(this ILogger logger, string? errors);

    /// <summary>AddSupportTicket write failed.</summary>
    [LoggerMessage(EventId = 2044, Level = LogLevel.Warning,
        Message = "AddSupportTicket failed: {Errors}")]
    public static partial void AddSupportTicketFailed(this ILogger logger, string? errors);

    /// <summary>AddKiosk write failed.</summary>
    [LoggerMessage(EventId = 2045, Level = LogLevel.Warning,
        Message = "AddKiosk failed: {Errors}")]
    public static partial void AddKioskFailed(this ILogger logger, string? errors);

    /// <summary>AddMerchant write failed.</summary>
    [LoggerMessage(EventId = 2046, Level = LogLevel.Warning,
        Message = "AddMerchant failed: {Errors}")]
    public static partial void AddMerchantFailed(this ILogger logger, string? errors);

    /// <summary>ResolveException write failed.</summary>
    [LoggerMessage(EventId = 2047, Level = LogLevel.Warning,
        Message = "ResolveException failed: {Errors}")]
    public static partial void ResolveExceptionFailed(this ILogger logger, string? errors);

    /// <summary>Kiosk security token issuance failed.</summary>
    [LoggerMessage(EventId = 2048, Level = LogLevel.Warning,
        Message = "IssueKioskSecurityToken failed for kiosk {KioskId}")]
    public static partial void IssueKioskSecurityTokenFailed(this ILogger logger, Guid? kioskId);

    /// <summary>Expenses table not present; TotalExpense report renders empty.</summary>
    [LoggerMessage(EventId = 2049, Level = LogLevel.Information,
        Message = "TotalExpense: expenses table not present in schema yet - returning empty list.")]
    public static partial void TotalExpenseTableMissing(this ILogger logger);

    /// <summary>Custom role creation rejected.</summary>
    [LoggerMessage(EventId = 2050, Level = LogLevel.Warning,
        Message = "CreateCustomRole rejected: {Errors}")]
    public static partial void CreateCustomRoleRejected(this ILogger logger, string? errors);

    /// <summary>CSV export called with an unknown client kind.</summary>
    [LoggerMessage(EventId = 2051, Level = LogLevel.Warning,
        Message = "ExportClientsCsv called with invalid kind '{Kind}'")]
    public static partial void ExportClientsCsvInvalidKind(this ILogger logger, string? kind);

    /// <summary>AddVoucher write failed.</summary>
    [LoggerMessage(EventId = 2052, Level = LogLevel.Warning,
        Message = "AddVoucher failed: {Errors}")]
    public static partial void AddVoucherFailed(this ILogger logger, string? errors);

    // ── Tenant settings (2070–2089) ─────────────────────────────────────────

    /// <summary>SaveGeneral failed.</summary>
    [LoggerMessage(EventId = 2070, Level = LogLevel.Error,
        Message = "SaveGeneral failed for tenant={Tenant}")]
    public static partial void SaveGeneralFailed(this ILogger logger, Exception exception, Guid? tenant);

    /// <summary>SaveBranding failed.</summary>
    [LoggerMessage(EventId = 2071, Level = LogLevel.Error,
        Message = "SaveBranding failed for tenant={Tenant}")]
    public static partial void SaveBrandingFailed(this ILogger logger, Exception exception, Guid? tenant);

    /// <summary>SaveFeatures failed.</summary>
    [LoggerMessage(EventId = 2072, Level = LogLevel.Error,
        Message = "SaveFeatures failed for tenant={Tenant}")]
    public static partial void SaveFeaturesFailed(this ILogger logger, Exception exception, Guid? tenant);

    /// <summary>SaveRetention failed.</summary>
    [LoggerMessage(EventId = 2073, Level = LogLevel.Error,
        Message = "SaveRetention failed for tenant={Tenant}")]
    public static partial void SaveRetentionFailed(this ILogger logger, Exception exception, Guid? tenant);

    /// <summary>Activation key re-issue requested.</summary>
    [LoggerMessage(EventId = 2074, Level = LogLevel.Information,
        Message = "Activation key re-issue requested - tenant={Tenant} actor={Actor} reason={Reason}")]
    public static partial void ActivationKeyReissueRequested(this ILogger logger, Guid? tenant, Guid? actor, string? reason);

    /// <summary>General settings saved.</summary>
    [LoggerMessage(EventId = 2075, Level = LogLevel.Information,
        Message = "SaveGeneral: tenant={Tenant} actor={Actor} tier={Tier} region={Region}")]
    public static partial void GeneralSettingsSaved(this ILogger logger, Guid? tenant, Guid? actor, string? tier, string? region);

    /// <summary>Branding saved.</summary>
    [LoggerMessage(EventId = 2076, Level = LogLevel.Information,
        Message = "SaveBranding: tenant={Tenant} actor={Actor} accent={Accent}")]
    public static partial void BrandingSaved(this ILogger logger, Guid? tenant, Guid? actor, string? accent);

    /// <summary>Feature flags saved.</summary>
    [LoggerMessage(EventId = 2077, Level = LogLevel.Information,
        Message = "SaveFeatures: tenant={Tenant} actor={Actor} flags={Count}")]
    public static partial void FeatureFlagsSaved(this ILogger logger, Guid? tenant, Guid? actor, int? count);

    /// <summary>Retention buckets saved.</summary>
    [LoggerMessage(EventId = 2078, Level = LogLevel.Information,
        Message = "SaveRetention: tenant={Tenant} actor={Actor} pii={Pii} photo={Photo} tx={Tx} audit={Audit}")]
    public static partial void RetentionSaved(this ILogger logger, Guid? tenant, Guid? actor, int? pii, int? photo, int? tx, int? audit);

    // ── Invitations (2090–2099) ─────────────────────────────────────────────

    /// <summary>Invitation send failed (recipient never logged).</summary>
    [LoggerMessage(EventId = 2090, Level = LogLevel.Error,
        Message = "Failed to send an invitation (recipient omitted from logs per PII policy).")]
    public static partial void InvitationSendFailed(this ILogger logger, Exception exception);

    /// <summary>Invitation resend failed.</summary>
    [LoggerMessage(EventId = 2091, Level = LogLevel.Error,
        Message = "Failed to resend invitation {Id}")]
    public static partial void InvitationResendFailed(this ILogger logger, Exception exception, Guid? id);

    /// <summary>Invitation created (invitee email intentionally omitted).</summary>
    [LoggerMessage(EventId = 2092, Level = LogLevel.Information,
        Message = "Invitation {InviteId} created (tenant {TenantId}, role {RoleId}); recipient omitted per PII policy. Expires {Expires}.")]
    public static partial void InvitationCreated(this ILogger logger, Guid? inviteId, Guid? tenantId, Guid? roleId, DateTimeOffset? expires);

    /// <summary>Dev/demo console email sink (recipient omitted; body may carry action links).</summary>
    [LoggerMessage(EventId = 2093, Level = LogLevel.Information,
        Message = "EMAIL (console-only sender; recipient omitted per PII policy)\nSubject: {Subject}\n{Body}")]
    public static partial void ConsoleEmailWritten(this ILogger logger, string? subject, string? body);

    // ── Licensing (2100–2119) ───────────────────────────────────────────────

    /// <summary>Owner user provisioned from the licence payload (email omitted).</summary>
    [LoggerMessage(EventId = 2100, Level = LogLevel.Information,
        Message = "Provisioned Owner for tenant {TenantId} ({Legal}) from licence.")]
    public static partial void OwnerProvisionedFromLicense(this ILogger logger, Guid? tenantId, string? legal);

    /// <summary>A stored license file could not be parsed.</summary>
    [LoggerMessage(EventId = 2101, Level = LogLevel.Error,
        Message = "Stored license at {Path} is corrupt - ignoring.")]
    public static partial void StoredLicenseCorrupt(this ILogger logger, Exception exception, string? path);

    /// <summary>JWKS refreshed from the CRM.</summary>
    [LoggerMessage(EventId = 2102, Level = LogLevel.Information,
        Message = "JWKS refreshed from {Url}.")]
    public static partial void JwksRefreshed(this ILogger logger, string? url);

    /// <summary>JWKS refresh failed; last-known keys stay in effect.</summary>
    [LoggerMessage(EventId = 2103, Level = LogLevel.Warning,
        Message = "JWKS refresh failed - continuing with last-known keys (pinned only if first run).")]
    public static partial void JwksRefreshFailed(this ILogger logger, Exception exception);

    /// <summary>Configured pinned JWKS could not be parsed.</summary>
    [LoggerMessage(EventId = 2104, Level = LogLevel.Error,
        Message = "Pinned JWKS is malformed - verifier will reject all tokens until JWKS is fetched.")]
    public static partial void PinnedJwksMalformed(this ILogger logger, Exception exception);

    /// <summary>First license refresh at startup failed.</summary>
    [LoggerMessage(EventId = 2105, Level = LogLevel.Warning,
        Message = "Initial license refresh failed - the app will still start; pinned keys (if any) will be used until a later refresh succeeds.")]
    public static partial void InitialLicenseRefreshFailed(this ILogger logger, Exception exception);

    /// <summary>License activated and bound to this install.</summary>
    [LoggerMessage(EventId = 2106, Level = LogLevel.Information,
        Message = "License activated for tenant {TenantId} ({LegalName}) plan={Plan} cap={Cap}.")]
    public static partial void LicenseActivated(this ILogger logger, Guid? tenantId, string? legalName, string? plan, int? cap);

    /// <summary>Revocation list refreshed.</summary>
    [LoggerMessage(EventId = 2107, Level = LogLevel.Information,
        Message = "Revocation list refreshed: {Count} entries.")]
    public static partial void RevocationListRefreshed(this ILogger logger, int? count);

    /// <summary>Revocation list refresh failed; last-known list stays in effect.</summary>
    [LoggerMessage(EventId = 2108, Level = LogLevel.Warning,
        Message = "Revocation list refresh failed - continuing with last-known list.")]
    public static partial void RevocationListRefreshFailed(this ILogger logger, Exception exception);

    // ── Host / seeding (2120–2139) ──────────────────────────────────────────

    /// <summary>Loud startup banner for the known security stubs.</summary>
    [LoggerMessage(EventId = 2120, Level = LogLevel.Warning,
        Message = "SECURITY STUBS ACTIVE: customer PII encryption is stubbed (GK-SEC-1) and wallet PIN hashing uses bare SHA-256 (GK-SEC-2). Do not ship to production.")]
    public static partial void SecurityStubsActive(this ILogger logger);

    /// <summary>Development seeding failed; the app continues to start.</summary>
    [LoggerMessage(EventId = 2121, Level = LogLevel.Error,
        Message = "Dev seeding failed. The app will still start; sign in may not work until the DB is reachable and 0028 has been applied.")]
    public static partial void DevSeedingFailed(this ILogger logger, Exception exception);

    /// <summary>GK-DEV tenant seeded.</summary>
    [LoggerMessage(EventId = 2122, Level = LogLevel.Information,
        Message = "Seeded tenant GK-DEV ({TenantId})")]
    public static partial void DevTenantSeeded(this ILogger logger, Guid? tenantId);

    /// <summary>Dev admin user seeded (credentials are the documented dev defaults).</summary>
    [LoggerMessage(EventId = 2123, Level = LogLevel.Information,
        Message = "Seeded dev admin user {UserId} with the documented dev credentials.")]
    public static partial void DevAdminSeeded(this ILogger logger, Guid? userId);

    /// <summary>Dev admin user could not be created.</summary>
    [LoggerMessage(EventId = 2124, Level = LogLevel.Error,
        Message = "Failed to seed admin user: {Errors}")]
    public static partial void DevAdminSeedFailed(this ILogger logger, string? errors);

    /// <summary>Dev admin user already present.</summary>
    [LoggerMessage(EventId = 2125, Level = LogLevel.Information,
        Message = "Dev admin user {UserId} already exists.")]
    public static partial void DevAdminExists(this ILogger logger, Guid? userId);

    /// <summary>Reference tenant EGB-AE-001 seeded.</summary>
    [LoggerMessage(EventId = 2126, Level = LogLevel.Information,
        Message = "Seeded tenant EGB-AE-001 (Emirates Gold Bank)")]
    public static partial void EgbTenantSeeded(this ILogger logger);

    /// <summary>Demo activation key seeded (prefix only; never the full key).</summary>
    [LoggerMessage(EventId = 2127, Level = LogLevel.Information,
        Message = "Seeded demo activation key with prefix {KeyPrefix} (tenant=EGB-AE-001).")]
    public static partial void DemoActivationKeySeeded(this ILogger logger, string? keyPrefix);

    // ── Reporting fallbacks (2140–2159) ─────────────────────────────────────

    /// <summary>Daily-sales matview missing; report renders empty.</summary>
    [LoggerMessage(EventId = 2140, Level = LogLevel.Warning,
        Message = "reporting.mv_daily_sales_summary is missing. Falling back to empty result. Apply db/0401_matviews.sql and CALL reporting.refresh_all_matviews().")]
    public static partial void DailySalesMatviewMissing(this ILogger logger, Exception exception);

    /// <summary>Inventory matview missing; inventory renders empty.</summary>
    [LoggerMessage(EventId = 2141, Level = LogLevel.Warning,
        Message = "reporting.mv_daily_inventory_summary is missing - returning empty inventory.")]
    public static partial void InventoryMatviewMissing(this ILogger logger, Exception exception);

    /// <summary>Expenses view missing; expenses flagged as missing.</summary>
    [LoggerMessage(EventId = 2142, Level = LogLevel.Warning,
        Message = "reporting.expenses_per_day is missing - marking expenses table missing.")]
    public static partial void ExpensesViewMissing(this ILogger logger, Exception exception);

    /// <summary>TotalExpense query failed; expenses flagged as missing.</summary>
    [LoggerMessage(EventId = 2143, Level = LogLevel.Warning,
        Message = "TotalExpense report query failed; flagging table missing.")]
    public static partial void TotalExpenseQueryFailed(this ILogger logger, Exception exception);

    /// <summary>Inventory matview missing; Worth report renders empty.</summary>
    [LoggerMessage(EventId = 2144, Level = LogLevel.Warning,
        Message = "reporting.mv_daily_inventory_summary is missing - returning empty Worth report.")]
    public static partial void WorthInventoryMatviewMissing(this ILogger logger, Exception exception);

    /// <summary>Metal-rates table missing; rates default to zero.</summary>
    [LoggerMessage(EventId = 2145, Level = LogLevel.Warning,
        Message = "pricing.metal_rates is missing - rates default to zero.")]
    public static partial void MetalRatesMissing(this ILogger logger, Exception exception);

    /// <summary>Payment tables missing; SalesPayout report renders empty.</summary>
    [LoggerMessage(EventId = 2146, Level = LogLevel.Warning,
        Message = "payment.payments or payment.payouts missing - returning empty SalesPayout report.")]
    public static partial void PaymentsTablesMissing(this ILogger logger, Exception exception);

    /// <summary>Profit matview missing; ExpectedProfit report renders empty.</summary>
    [LoggerMessage(EventId = 2147, Level = LogLevel.Warning,
        Message = "reporting.mv_daily_profit_summary is missing - returning empty ExpectedProfit report.")]
    public static partial void ExpectedProfitMatviewMissing(this ILogger logger, Exception exception);

    /// <summary>Profit matview missing; ProfitAfterExpense report renders empty.</summary>
    [LoggerMessage(EventId = 2148, Level = LogLevel.Warning,
        Message = "reporting.mv_daily_profit_summary is missing - returning empty ProfitAfterExpense report.")]
    public static partial void ProfitAfterExpenseMatviewMissing(this ILogger logger, Exception exception);

    // ── Roles & permissions (2160–2169) ─────────────────────────────────────

    /// <summary>Custom role created.</summary>
    [LoggerMessage(EventId = 2160, Level = LogLevel.Information,
        Message = "Custom role {Code} created with {Count} permissions by {Actor}")]
    public static partial void CustomRoleCreated(this ILogger logger, string? code, int? count, Guid? actor);

    /// <summary>Role permission set updated.</summary>
    [LoggerMessage(EventId = 2161, Level = LogLevel.Information,
        Message = "Role {RoleId} permissions updated: +{Added} -{Removed} by {Actor}")]
    public static partial void RolePermissionsUpdated(this ILogger logger, Guid? roleId, int? added, int? removed, Guid? actor);

    /// <summary>Custom role soft-deleted.</summary>
    [LoggerMessage(EventId = 2162, Level = LogLevel.Information,
        Message = "Custom role {RoleId} ({Code}) soft-deleted by {Actor}")]
    public static partial void CustomRoleDeleted(this ILogger logger, Guid? roleId, string? code, Guid? actor);
}
