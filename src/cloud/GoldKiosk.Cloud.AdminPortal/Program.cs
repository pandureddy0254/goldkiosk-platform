using GoldKiosk.Cloud.AdminPortal.Logging;
using System.Globalization;
using GoldKiosk.Cloud.AdminPortal.Services;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Cloud.AdminPortal.Services.Email;
using GoldKiosk.Cloud.AdminPortal.Services.Invitations;
using GoldKiosk.Cloud.AdminPortal.Services.Licensing;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Identity;
using GoldKiosk.Infrastructure.Interceptors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Npgsql legacy timestamp behavior is a solution-wide invariant (CLAUDE.md §3).
// This MUST be the very first executable statement so no Npgsql type mapping is
// initialized before the switch is set.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// ─── Serilog: console + rolling file (local-first logging standard) ─────────
builder.Host.UseSerilog((context, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .WriteTo.File(
        Path.Combine("logs", "admin-portal-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        formatProvider: CultureInfo.InvariantCulture));

// ─── Aspire service defaults: OTel, health checks, resilience, discovery ────
builder.AddServiceDefaults();

// ─── Forwarded headers (reverse proxy terminates SSL upstream) ──────────────
// Must be configured before MVC so the auth middleware reads the proxied
// scheme; without it Request.IsHttps is false behind the proxy and the
// "Secure" cookie flag drops the auth cookie. KnownProxies/Networks are
// cleared because the proxy sits inside the platform's trusted network.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

// ─── MVC ────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// ─── Configuration / connection string ──────────────────────────────────────
// appsettings.json intentionally carries an empty "ConnectionStrings:Default";
// the real value comes from user-secrets in dev and Key Vault in production.
var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'Default' is missing. Set it via user-secrets:\n" +
        "  dotnet user-secrets set \"ConnectionStrings:Default\" " +
        "\"Host=localhost;Port=5432;Database=goldkiosk_local;Username=postgres;Password=<your_password>\"");
}

// ─── Current-user + interceptors ────────────────────────────────────────────
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IUserPermissionService, UserPermissionService>();
builder.Services.AddScoped<TenantContextInterceptor>();
builder.Services.AddScoped<AuditActorInterceptor>();

// ─── Domain services ────────────────────────────────────────────────────────
builder.Services.AddScoped<IKioskService, KioskService>();
builder.Services.AddScoped<IScreenSaverService, ScreenSaverService>();
builder.Services.AddScoped<IPartnerApiCredentialService, PartnerApiCredentialService>();
builder.Services.AddUserManagementServices();
builder.Services.AddCustomerManagement();
builder.Services.AddMerchantManagement();
builder.Services.AddVoucherManagement();
builder.Services.AddHelpDeskManagement();
builder.Services.AddFeedbackAndAudit();
builder.Services.AddMonitoring();
builder.Services.AddOperations();
builder.Services.AddSalesAndReports();
builder.Services.AddHome();
builder.Services.AddScoped<ITenantSettingsService, TenantSettingsService>();

// ─── Email + invitations ─────────────────────────────────────────────────────
// ConsoleEmailSender writes the message to the logger so dev/demo doesn't need
// an SMTP setup. Swap for a real provider adapter in a later ticket.
builder.Services.AddSingleton<IEmailSender, ConsoleEmailSender>();
builder.Services.AddScoped<IUserInvitationService, UserInvitationService>();

// ─── Licensing (CRM-issued AIKI- tokens) ─────────────────────────────────────
// JwksClient + RevocationClient cache mutable state (current public keys,
// current revocation set), so both must be singletons. They get a long-lived
// HttpClient via IHttpClientFactory — safe per the factory's documented
// contract.
builder.Services.Configure<LicensingOptions>(builder.Configuration.GetSection(LicensingOptions.SectionName));
builder.Services.AddHttpClient(nameof(JwksClient));
builder.Services.AddHttpClient(nameof(RevocationClient));
builder.Services.AddSingleton<ILicenseStore, FileLicenseStore>();
builder.Services.AddSingleton<IBrandingService, BrandingService>();
builder.Services.AddSingleton<JwksClient>(sp => new JwksClient(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(JwksClient)),
    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<LicensingOptions>>(),
    sp.GetRequiredService<ILogger<JwksClient>>()));
builder.Services.AddSingleton<RevocationClient>(sp => new RevocationClient(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(RevocationClient)),
    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<LicensingOptions>>(),
    sp.GetRequiredService<ILogger<RevocationClient>>()));
builder.Services.AddScoped<ILicenseService, LicenseService>();
builder.Services.AddHostedService<LicenseRefreshHostedService>();

// ─── EF Core / PostgreSQL ───────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>((sp, opts) =>
{
    opts.UseNpgsql(connectionString)
        .UseSnakeCaseNamingConvention()
        .AddInterceptors(
            sp.GetRequiredService<TenantContextInterceptor>(),
            sp.GetRequiredService<AuditActorInterceptor>());
});

// ─── ASP.NET Core Identity (core only — no Identity role system) ────────────
builder.Services
    .AddIdentityCore<AppUser>(opts =>
    {
        opts.User.RequireUniqueEmail = true;
        opts.SignIn.RequireConfirmedEmail = false;

        opts.Password.RequiredLength = 12;
        opts.Password.RequireDigit = true;
        opts.Password.RequireLowercase = true;
        opts.Password.RequireUppercase = true;
        opts.Password.RequireNonAlphanumeric = true;

        opts.Lockout.MaxFailedAccessAttempts = 5;
        opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Argon2id instead of the default PBKDF2 hasher.
builder.Services.AddScoped<IPasswordHasher<AppUser>, Argon2idPasswordHasher<AppUser>>();

// ─── Cookie authentication (OIDC is a later ticket) ─────────────────────────
builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, opts =>
    {
        opts.Cookie.Name = ".GoldKiosk.Auth";
        opts.LoginPath = "/Account/Login";
        opts.LogoutPath = "/Account/Logout";
        opts.AccessDeniedPath = "/Account/Denied";
        opts.SlidingExpiration = true;
        opts.ExpireTimeSpan = TimeSpan.FromHours(8);
        opts.Cookie.HttpOnly = true;
        opts.Cookie.SameSite = SameSiteMode.Lax;
        // SameAsRequest in Dev + Testing so the WebApplicationFactory test
        // server (plain http://localhost) can carry the auth cookie back.
        opts.Cookie.SecurePolicy = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing")
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    });

builder.Services.AddAuthorization(o =>
{
    // Every controller requires an authenticated user unless it opts out with
    // [AllowAnonymous] (e.g. AccountController).
    o.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

// ─── Loud startup warnings for known security stubs (fail loudly, not silently)
// TODO(GK-SEC-1): customer PII columns (*_enc) are stored as plain UTF-8 bytes.
// TODO(GK-SEC-2): customer wallet PINs are hashed with bare SHA-256.
app.Logger.SecurityStubsActive();

// ─── Dev-only seeding ───────────────────────────────────────────────────────
// Also enabled for the "Testing" env so the E2E test factory can sign in.
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    try
    {
        await DevStartupSeeder.SeedAsync(app.Services, logger);
    }
    catch (Exception ex)
    {
        logger.DevSeedingFailed(ex);
    }
}

// Must run before anything that reads scheme/host (auth, antiforgery, HSTS).
// In Dev / Testing this is a no-op because the headers aren't present.
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Skip HTTPS redirection under the Testing environment so the
// WebApplicationFactory test server (which serves only http://localhost) does
// not 307 every request.
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.MapStaticAssets();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// License gate: bind the install to a CRM-issued license before serving app
// routes. Bypasses /License, /activate, /Account, /branding, /health, and
// static assets so unlicensed installs can still reach the activation page.
app.UseMiddleware<LicenseGateMiddleware>();

// /health and /alive come from MapDefaultEndpoints (Development only, per the
// ServiceDefaults standard). Production health probes are a deployment ticket.
app.MapDefaultEndpoints();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();

/// <summary>
/// Exposed so <c>WebApplicationFactory&lt;Program&gt;</c> can host the portal
/// in the E2E test project.
/// </summary>
public partial class Program;
