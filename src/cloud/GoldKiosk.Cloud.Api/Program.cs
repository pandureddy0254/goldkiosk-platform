using System.Text.Json;
using FluentValidation;
using GoldKiosk.Cloud.Api;
using GoldKiosk.Cloud.Api.Auth;
using GoldKiosk.Cloud.Api.Endpoints;
using GoldKiosk.Cloud.Api.Errors;
using GoldKiosk.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Serilog;

// Postgres convention (CLAUDE.md §3): the Npgsql legacy-timestamp switch is set before
// anything else runs — global, immutable, never altered as a side effect.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Serilog: console + daily rolling file; sinks configured in appsettings. Cloud-bound
// telemetry stays gated per configuration-and-operations §4.
builder.Host.UseSerilog(static (context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.AddServiceDefaults();
builder.AddDefaultOpenApi();

// snake_case JSON on the wire, matching the Contracts wire-format decision.
builder.Services.ConfigureHttpJsonOptions(static options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<CloudExceptionHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddCloudApiOptions();
builder.Services.AddCloudApiServices(builder.Configuration);

// Kiosk machines authenticate with a bearer JWT (Code + PIN login); every endpoint
// besides the login declares the kiosk policy explicitly.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.ConfigureOptions<ConfigureJwtBearerOptions>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthorizationPolicies.Kiosk, static policy => policy
        .RequireAuthenticatedUser()
        .RequireRole(KioskClaimTypes.KioskRoleValue));

var app = builder.Build();

app.UseExceptionHandler();

app.MapDefaultEndpoints();
app.MapDefaultOpenApi();

app.UseAuthentication();
app.UseAuthorization();

RouteGroupBuilder api = app.MapGroup("/api/v1");
api.MapAuthEndpoints();
api.MapRatesEndpoints();
api.MapOffersEndpoints();
api.MapItemsEndpoints();
api.MapLiveAgentEndpoints();
api.MapTransactionsEndpoints();
api.MapKioskEndpoints();
api.MapContentEndpoints();

app.Run();

/// <summary>Marker type so WebApplicationFactory-based integration tests can target this host.</summary>
public partial class Program;
