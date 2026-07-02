using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GoldKiosk.ServiceDefaults;

/// <summary>
/// Default OpenAPI + Swagger UI wiring for GoldKiosk web API hosts (cloud-api, kiosk-api).
/// Document generation uses the first-party ASP.NET Core OpenAPI services; the interactive
/// Swagger UI is served in Development only (ADR 0001).
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>
    /// Registers OpenAPI document generation for the host. Call alongside
    /// <c>AddServiceDefaults()</c> in every web API host.
    /// </summary>
    /// <typeparam name="TBuilder">The host application builder type.</typeparam>
    /// <param name="builder">The host application builder to add OpenAPI services to.</param>
    /// <returns>The same <paramref name="builder"/> for chaining.</returns>
    public static TBuilder AddDefaultOpenApi<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOpenApi();

        return builder;
    }

    /// <summary>
    /// Maps the OpenAPI document at <c>/openapi/v1.json</c> and the Swagger UI at
    /// <c>/swagger</c>. Both are exposed in Development environments only; production
    /// exposure requires an explicit decision per ADR 0001.
    /// </summary>
    /// <param name="app">The web application to map the OpenAPI endpoints on.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static WebApplication MapDefaultOpenApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        app.MapOpenApi();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "GoldKiosk API v1");
        });

        return app;
    }
}
