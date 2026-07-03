using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Kiosk.Api.Logging;
using GoldKiosk.Kiosk.Devices.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GoldKiosk.Kiosk.Api.Errors;

/// <summary>
/// Global exception → ProblemDetails mapping (payload samples §15): device failures
/// surface as <c>device.unavailable</c> (503); anything else is an opaque 500. Details
/// never leak to the wire; the full exception goes to the local log only.
/// </summary>
/// <param name="problemDetailsService">Writes the RFC 7807 body.</param>
/// <param name="logger">The host logger.</param>
public sealed class KioskExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<KioskExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        (int statusCode, string type, string title) = exception switch
        {
            SimulatedDeviceFaultException or DeviceNotWiredException =>
                (StatusCodes.Status503ServiceUnavailable, ProblemTypes.DeviceUnavailable,
                    "A required device is unavailable"),
            _ =>
                (StatusCodes.Status500InternalServerError, "about:blank",
                    "An unexpected error occurred"),
        };

        logger.UnhandledException(exception, httpContext.Request.Path.ToString());

        httpContext.Response.StatusCode = statusCode;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Type = type,
                Title = title,
            },
        });
    }
}
