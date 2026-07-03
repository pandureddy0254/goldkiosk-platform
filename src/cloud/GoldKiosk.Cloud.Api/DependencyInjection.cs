using GoldKiosk.Cloud.Api.Auth;
using GoldKiosk.Cloud.Api.Options;
using GoldKiosk.Cloud.Api.Services.Ai;
using GoldKiosk.Cloud.Api.Services.Content;
using GoldKiosk.Cloud.Api.Services.Items;
using GoldKiosk.Cloud.Api.Services.LiveAgent;
using GoldKiosk.Cloud.Api.Services.Offers;
using GoldKiosk.Cloud.Api.Services.Rates;
using GoldKiosk.Cloud.Api.Services.Transactions;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Identity;
using GoldKiosk.Infrastructure.Interceptors;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GoldKiosk.Cloud.Api;

/// <summary>
/// Cloud.Api composition: options (all sections validate at startup — the host fails
/// fast, it does not limp) and service registrations, one extension per concern.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Registers and validates every Cloud.Api options section.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection for chaining.</returns>
    public static IServiceCollection AddCloudApiOptions(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<GoldApiOptions>()
            .BindConfiguration(GoldApiOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<RegionOptions>()
            .BindConfiguration(RegionOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<OffersOptions>()
            .BindConfiguration(OffersOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<AiServiceOptions>()
            .BindConfiguration(AiServiceOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<LiveAgentOptions>()
            .BindConfiguration(LiveAgentOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<KioskConfigOptions>()
            .BindConfiguration(KioskConfigOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    /// <summary>Registers the database, auth, rates, offers, AI, live-agent, transaction and content services.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The host configuration.</param>
    /// <returns>The same collection for chaining.</returns>
    public static IServiceCollection AddCloudApiServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // ── Kiosk principal + RLS tenant context ─────────────────────────────
        services.AddHttpContextAccessor();
        services.AddScoped<CurrentKioskService>();
        services.AddScoped<ICurrentUserService>(static sp => sp.GetRequiredService<CurrentKioskService>());
        services.AddScoped<TenantContextInterceptor>();

        // ── Database (snake_case + RLS interceptor — Postgres conventions §3) ─
        string connectionString = configuration.GetConnectionString("goldkiosk")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:goldkiosk is not configured. Set it via user-secrets in dev.");
        services.AddDbContext<AppDbContext>((sp, options) => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<TenantContextInterceptor>()));

        // ── Kiosk auth (Argon2id PIN verify → JWT) ───────────────────────────
        services.AddScoped<IPasswordHasher<AppUser>, Argon2idPasswordHasher<AppUser>>();
        services.AddScoped<IKioskAuthService, KioskAuthService>();

        // ── Rates (GoldAPI.io → pricing.metal_rates, daily sync worker) ──────
        services.AddHttpClient(GoldApiPriceProvider.HttpClientName);
        services.AddSingleton<IMetalPriceProvider, GoldApiPriceProvider>();
        services.AddScoped<IMetalRateReader, MetalRateReader>();
        services.AddHostedService<GoldRateSyncService>();

        // ── Offers (rates × margin, Domain floor rounding, quote store) ──────
        services.AddSingleton<OfferQuoteStore>();
        services.AddScoped<IOfferService, OfferService>();
        services.AddSingleton<IOfferExplanationService, TemplatedOfferExplanationService>();

        // ── AI item verification (in-house goldkiosk-ai; no LLM SDK) ─────────
        // The default resilience handler caps requests well below the AI service's worst
        // case; this client relies on its own long timeout with a single attempt instead
        // (matching the legacy provider's behavior — degrade-open covers failures).
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers is the supported opt-out for a single
        // client whose 120 s calls exceed the fleet-default pipeline's timeouts; the API is marked
        // experimental but shipping in-box — revisit when it stabilizes.
        services.AddHttpClient<IItemClassifier, GoldKioskAiClient>(static (sp, client) =>
            {
                AiServiceOptions ai = sp.GetRequiredService<IOptions<AiServiceOptions>>().Value;
                client.BaseAddress = new Uri(ai.BaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(ai.TimeoutSeconds);
            })
            .RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001
        services.AddScoped<IItemAnalysisService, ItemAnalysisService>();

        // ── Live-agent escalation (fail-closed; in-memory until GK-LA-1) ─────
        services.AddSingleton<ItemReviewStore>();
        services.AddSingleton<AgentAvailabilityStore>();

        // ── Transactions (idempotent MAKE-SELL replacement) ──────────────────
        services.AddScoped<ITransactionRecorder, TransactionRecorder>();

        // ── Tenant reference content ─────────────────────────────────────────
        services.AddScoped<IKioskContentService, KioskContentService>();

        return services;
    }
}
