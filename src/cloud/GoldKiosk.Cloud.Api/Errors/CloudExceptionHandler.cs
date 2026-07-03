using GoldKiosk.Cloud.Api.Logging;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GoldKiosk.Cloud.Api.Errors;

/// <summary>
/// Global exception → ProblemDetails mapping. Everything unexpected is an opaque 500;
/// details never leak to the wire — the full exception goes to the server log only.
/// </summary>
/// <param name="problemDetailsService">Writes the RFC 7807 body.</param>
/// <param name="logger">The host logger.</param>
public sealed class CloudExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<CloudExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        logger.UnhandledException(exception, httpContext.Request.Path.ToString());

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Type = "about:blank",
                Title = "An unexpected error occurred",
            },
        });
    }
}
