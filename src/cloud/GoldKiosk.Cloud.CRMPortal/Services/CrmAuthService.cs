using System.Security.Claims;
using GoldKiosk.Cloud.CRMPortal.Data;
using GoldKiosk.Cloud.CRMPortal.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.CRMPortal.Services;

/// <summary>Outcome of a sign-in attempt.</summary>
/// <param name="Success">True when the credentials matched and the cookie was issued.</param>
/// <param name="Error">User-safe failure message when <paramref name="Success"/> is false.</param>
public record SignInResult(bool Success, string? Error);

/// <summary>
/// Cookie-based authentication for CRM staff. Verifies credentials against the
/// <c>crm.verify_login</c> SECURITY DEFINER RPC (bcrypt comparison happens in the
/// DB via pgcrypto, matching the seed + crm.create_member hashing) and issues an
/// ASP.NET Core cookie principal.
/// </summary>
public interface ICrmAuthService
{
    /// <summary>Verifies credentials and issues the auth cookie on success.</summary>
    /// <param name="httpContext">The current request context to sign in on.</param>
    /// <param name="email">Sign-in email.</param>
    /// <param name="password">Sign-in password (never logged).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<SignInResult> SignInAsync(HttpContext httpContext, string email, string password, CancellationToken ct = default);

    /// <summary>Clears the auth cookie for the current session.</summary>
    /// <param name="httpContext">The current request context to sign out of.</param>
    Task SignOutAsync(HttpContext httpContext);
}

/// <summary>Default <see cref="ICrmAuthService"/> backed by the crm.verify_login RPC.</summary>
public sealed class CrmAuthService : ICrmAuthService
{
    private readonly CrmDbContext _db;
    private readonly ILogger<CrmAuthService> _log;

    /// <summary>Initializes the service with the CRM database context and a logger.</summary>
    /// <param name="db">CRM database context.</param>
    /// <param name="log">Logger (PII such as email addresses is never written to it).</param>
    public CrmAuthService(CrmDbContext db, ILogger<CrmAuthService> log)
    {
        _db = db;
        _log = log;
    }

    // Shape returned by crm.verify_login(p_email, p_password): 0 or 1 row.
    private sealed record LoginRow(Guid Id, string FullName, string Role);

    /// <inheritdoc/>
    public async Task<SignInResult> SignInAsync(HttpContext httpContext, string email, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return new SignInResult(false, "Email and password are required.");
        }

        try
        {
            // crm.verify_login is a SECURITY DEFINER RPC that returns the matching
            // profile (id, full_name, role) iff the bcrypt password matches, else 0 rows.
            // {0}/{1} become parameterised placeholders ($1/$2) — never string-concatenated.
            // Columns come back snake_case (id, full_name, role); the snake_case
            // naming convention maps them to LoginRow.Id/FullName/Role — so we must
            // NOT alias to PascalCase here.
            var rows = await _db.Database
                .SqlQueryRaw<LoginRow>(
                    "SELECT id, full_name, role FROM crm.verify_login({0}, {1})",
                    email, password)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var row = rows.FirstOrDefault();
            if (row is null)
            {
                return new SignInResult(false, "Invalid email or password.");
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, row.Id.ToString()),
                new(ClaimTypes.Name, row.FullName ?? string.Empty),
                new(ClaimTypes.Role, row.Role ?? string.Empty),
                new(ClaimTypes.Email, email),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal).ConfigureAwait(false);
            return new SignInResult(true, null);
        }
        catch (Exception ex)
        {
            // Don't surface raw DB errors (schema/column names) to the user, and never
            // log the attempted email address — logs must stay free of PII.
            _log.SignInVerificationFailed(ex);
            return new SignInResult(false, "Sign-in is temporarily unavailable. Please try again.");
        }
    }

    /// <inheritdoc/>
    public Task SignOutAsync(HttpContext httpContext) =>
        httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
}
