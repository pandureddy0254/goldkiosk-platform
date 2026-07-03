using System.Text.Json;
using FluentValidation;
using GoldKiosk.Kiosk.Api.Devices;
using GoldKiosk.Kiosk.Api.Endpoints;
using GoldKiosk.Kiosk.Api.Errors;
using GoldKiosk.Kiosk.Api.Hosting;
using GoldKiosk.Kiosk.Api.Hub;
using GoldKiosk.Kiosk.Api.Idempotency;
using GoldKiosk.Kiosk.Api.Orchestration;
using GoldKiosk.Kiosk.Core.Analysis;
using GoldKiosk.Kiosk.Core.Options;
using GoldKiosk.Kiosk.Core.Orchestration;
using GoldKiosk.Kiosk.Core.Persistence;
using GoldKiosk.Kiosk.Core.Pricing;
using GoldKiosk.Kiosk.Core.Sessions;
using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.ServiceDefaults;
using Microsoft.Extensions.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Local-first logging: console + daily rolling file; sinks configured in appsettings
// (configuration-and-operations §4). Cloud telemetry stays gated behind the whitelist.
builder.Host.UseSerilog(static (context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.AddServiceDefaults();
builder.AddDefaultOpenApi();

// snake_case JSON on the wire — REST and SignalR alike (design note §4).
builder.Services.ConfigureHttpJsonOptions(static options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
});
builder.Services.AddSignalR().AddJsonProtocol(static options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.PayloadSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<KioskExceptionHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Options: every section validates at startup — an invalid kiosk fails fast, it does not limp.
builder.Services.AddOptions<KioskOptions>()
    .BindConfiguration(KioskOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<FeaturesOptions>()
    .BindConfiguration(FeaturesOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<AnalysisOptions>()
    .BindConfiguration(AnalysisOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<MockRatesOptions>()
    .BindConfiguration(MockRatesOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<DevicesOptions>()
    .BindConfiguration(DevicesOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton(TimeProvider.System);

// Device composition happens once at startup (ADR 0004): Simulated* or Real* per key from
// the resolved mode; the effective mode is logged by DeviceLifecycleService.
builder.Services.AddSingleton<IDeviceRegistry>(static sp => DeviceComposition.CreateRegistry(
    sp.GetRequiredService<IOptions<DevicesOptions>>().Value,
    sp.GetRequiredService<TimeProvider>(),
    sp.GetRequiredService<IConfiguration>()));

builder.Services.AddSingleton<SessionRegistry>();
builder.Services.AddSingleton<IdempotencyStore>();
builder.Services.AddSingleton<ISessionStore>(static sp => new FileSessionStore(
    sp.GetRequiredService<IOptions<KioskOptions>>().Value,
    sp.GetRequiredService<TimeProvider>()));
builder.Services.AddSingleton<IOfferCalculator>(static sp => new MockOfferCalculator(
    sp.GetRequiredService<IOptions<MockRatesOptions>>().Value,
    sp.GetRequiredService<IOptions<KioskOptions>>().Value,
    sp.GetRequiredService<TimeProvider>()));
builder.Services.AddSingleton<IOfferExplainer, MockOfferExplainer>();
builder.Services.AddSingleton(static sp => new AnalysisPolicy(
    sp.GetRequiredService<IOptions<AnalysisOptions>>().Value));
builder.Services.AddSingleton<IKioskEventPublisher, SignalRKioskEventPublisher>();
builder.Services.AddSingleton<TransactionOrchestrator>();

builder.Services.AddHostedService<SessionRecoveryService>();
builder.Services.AddHostedService<DeviceLifecycleService>();
builder.Services.AddHostedService<IdleTimeoutService>();

var app = builder.Build();

app.UseExceptionHandler();

app.MapDefaultEndpoints();
app.MapDefaultOpenApi();

app.MapHub<KioskHub>("/hubs/kiosk");

RouteGroupBuilder api = app.MapGroup("/api/v1");
api.MapSessionEndpoints();
api.MapTrayEndpoints();
api.MapOfferEndpoints();
api.MapIdentityEndpoints();
api.MapContactEndpoints();
api.MapPayoutEndpoints();
api.MapSettlementEndpoints();
api.MapAgentEndpoints();
api.MapDeviceEndpoints();
api.MapRatesEndpoints();

app.Run();

/// <summary>Marker type so WebApplicationFactory-based integration tests can target this host.</summary>
public partial class Program;
