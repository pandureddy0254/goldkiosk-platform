using System.IO.Compression;
using GoldKiosk.Cloud.CRMPortal.Data;
using GoldKiosk.Cloud.CRMPortal.Infrastructure;
using GoldKiosk.Cloud.CRMPortal.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using Serilog;

// Npgsql legacy timestamp behaviour — must be the very first statement (CLAUDE.md §3):
// prevents "Cannot write DateTime with Kind=Unspecified to PostgreSQL type
// 'timestamp with time zone'" on every DateTime column round-trip.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Set the QuestPDF licence before any document is rendered.
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog (reads the "Serilog" section; console sink by default) ───────────
builder.Services.AddSerilog((services, lc) => lc
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services));

// ── Aspire service defaults: OTel, health checks, resilience, discovery ─────
builder.AddServiceDefaults();

// ── Forwarded headers (App Service front-end terminates SSL upstream) ────────
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

// ── Response compression (Brotli > Gzip) ────────────────────────────────────
builder.Services.AddResponseCompression(o =>
{
    o.EnableForHttps = true;
    o.Providers.Add<BrotliCompressionProvider>();
    o.Providers.Add<GzipCompressionProvider>();
    o.MimeTypes =
    [
        .. ResponseCompressionDefaults.MimeTypes,
        "application/javascript",
        "application/json",
        "image/svg+xml",
        "text/css",
        "text/html",
        "text/plain"
    ];
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

// ── Config / options (secrets arrive via user-secrets / Key Vault, never files) ──
builder.Services.Configure<LicenseOptions>(builder.Configuration.GetSection(LicenseOptions.SectionName));
builder.Services.AddSingleton<ILicenseSigner, LicenseSigner>();

builder.Services.Configure<SesOptions>(builder.Configuration.GetSection(SesOptions.SectionName));
builder.Services.AddSingleton<IEmailService, SesEmailService>();

builder.Services.Configure<ProposalOptions>(builder.Configuration.GetSection(ProposalOptions.SectionName));
builder.Services.AddSingleton<IProposalDocumentService, ProposalDocumentService>();

// ── Connection string ────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Connection string 'Default' is missing. Set it via user-secrets:\n" +
        "  dotnet user-secrets set \"ConnectionStrings:Default\" " +
        "\"Host=localhost;Port=5432;Database=goldkiosk_crm_local;Username=postgres;Password=<your_password>\"");

// ── HTTP context (needed by ICurrentUserService) ───────────────────────────
builder.Services.AddHttpContextAccessor();

// ── Current-user + connection interceptor ───────────────────────────────────
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<UserContextInterceptor>();

// ── EF Core / PostgreSQL (snake_case naming convention is mandatory) ─────────
builder.Services.AddDbContext<CrmDbContext>((sp, o) =>
{
    o.UseNpgsql(connectionString)
     .UseSnakeCaseNamingConvention()
     .AddInterceptors(sp.GetRequiredService<UserContextInterceptor>());
});

// ── Domain services (scoped per request) ────────────────────────────────────
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ICrmAuthService, CrmAuthService>();

// ── MVC + Razor ─────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews(o => o.Filters.Add<ThemeFilter>());

// ── Cookie authentication ────────────────────────────────────────────────────
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.LogoutPath = "/Account/SignOut";
        o.AccessDeniedPath = "/Account/Denied";
        o.Cookie.Name = ".GoldKiosk.CRM.Auth";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    });

// Every controller requires an authenticated user unless it opts out with
// [AllowAnonymous] (e.g. AccountController, the public license endpoints).
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

// ── Antiforgery ────────────────────────────────────────────────────────────
builder.Services.AddAntiforgery();

var app = builder.Build();

// Must run before anything that reads scheme/host (auth, antiforgery, HSTS).
app.UseForwardedHeaders();

// Response compression must run before any middleware that writes the body.
app.UseResponseCompression();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Static files: uploads are GUID-named (immutable content) → 1 year;
// fingerprinted assets (asp-append-version) → 1 week.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.Context.Request.Path.Value ?? "";
        ctx.Context.Response.Headers.CacheControl =
            path.Contains("/uploads/", StringComparison.OrdinalIgnoreCase)
                ? "public,max-age=31536000,immutable"
                : "public,max-age=604800";
    }
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Aspire dev endpoints: /health + /alive (Development only).
app.MapDefaultEndpoints();

// Public health probe for the production front-door target group. Development
// uses MapDefaultEndpoints' /health instead (mapping both would collide).
if (!app.Environment.IsDevelopment())
{
    app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
       .AllowAnonymous();
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
