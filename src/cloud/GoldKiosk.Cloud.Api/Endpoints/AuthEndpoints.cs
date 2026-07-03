using GoldKiosk.Cloud.Api.Auth;
using GoldKiosk.Cloud.Api.Errors;
using GoldKiosk.Cloud.Api.Validation;
using GoldKiosk.Contracts.V1.Cloud.Auth;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GoldKiosk.Cloud.Api.Endpoints;

/// <summary>
/// Kiosk machine authentication (Code + PIN → JWT). The only anonymous surface of the
/// Cloud API — everything else requires the kiosk policy.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>Maps the auth endpoints onto the versioned group.</summary>
    /// <param name="group">The <c>/api/v1</c> route group.</param>
    /// <returns>The same group for chaining.</returns>
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/auth/kiosk-login", LoginAsync)
            .AllowAnonymous()
            .AddEndpointFilter<ValidationFilter<KioskLoginRequest>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem();

        return group;
    }

    private static async Task<Results<Ok<KioskLoginResponse>, ProblemHttpResult>> LoginAsync(
        KioskLoginRequest request,
        IKioskAuthService auth,
        CancellationToken cancellationToken)
    {
        KioskLoginResponse? response =
            await auth.LoginAsync(request.Code.Trim(), request.Pin, cancellationToken);
        return response is null
            ? Problems.InvalidCredentials()
            : TypedResults.Ok(response);
    }
}
