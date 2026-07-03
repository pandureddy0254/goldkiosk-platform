using GoldKiosk.Cloud.CRMPortal.Data;
using GoldKiosk.Cloud.CRMPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.CRMPortal.Controllers.Api;

/// <summary>
/// Public license endpoints — no auth by design. Admin Dashboards (potentially
/// on-prem at banks) poll these to verify license tokens offline.
/// </summary>
[ApiController]
[Route("")]
[AllowAnonymous]
public class LicenseApiController : ControllerBase
{
    private readonly ILicenseSigner _signer;
    private readonly CrmDbContext _db;

    /// <summary>Initializes the controller with its collaborators.</summary>
    /// <param name="signer">License signer (source of the public keys).</param>
    /// <param name="db">CRM database context (revocation list).</param>
    public LicenseApiController(ILicenseSigner signer, CrmDbContext db)
    {
        _signer = signer;
        _db = db;
    }

    /// <summary>
    /// JWKS doc — same shape as /.well-known/jwks.json used by every OIDC verifier.
    /// Admin Dashboards embed this URL and pin the keys they trust.
    /// </summary>
    [HttpGet(".well-known/jwks.json")]
    [HttpGet("api/license/jwks")]
    public IActionResult Jwks()
    {
        var keys = _signer.PublicKeysByKid.Select(kv => new
        {
            kty = "OKP",
            crv = "Ed25519",
            kid = kv.Key,
            use = "sig",
            alg = "EdDSA",
            x = Base64Url(kv.Value),
        });
        Response.Headers.CacheControl = "public, max-age=300";
        return new JsonResult(new { keys, active_kid = _signer.ActiveKid });
    }

    /// <summary>
    /// Revocation list. Admin Dashboards poll this hourly and refuse any token
    /// whose AIKI-XXXXXXXX prefix appears here, regardless of signature.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("api/license/revoked.json")]
    public async Task<IActionResult> Revoked(CancellationToken ct)
    {
        // We expose only the prefix + reason + revoked_at — never the hash.
        var items = await _db.ActivationKeys.AsNoTracking()
            .Where(k => k.RevokedAt != null)
            .Select(k => new
            {
                prefix = k.KeyPrefix,
                revoked_at = k.RevokedAt,
                reason = k.RevokedReason,
            })
            .ToListAsync(ct);

        Response.Headers.CacheControl = "public, max-age=300";
        return new JsonResult(new { generated_at = DateTime.UtcNow, revoked = items });
    }

    /// <summary>
    /// Convenience for the Admin Dashboard rebuild — verify a token against the
    /// CRM's public keys.
    /// </summary>
    /// <param name="req">The token to verify.</param>
    [HttpPost("api/license/verify")]
    public IActionResult Verify([FromBody] VerifyRequest req)
    {
        var result = _signer.Verify(req?.Token ?? "");
        return new JsonResult(new
        {
            valid = result.Valid,
            error = result.Error,
            payload = result.Payload,
        });
    }

    /// <summary>Request body for <see cref="Verify"/>.</summary>
    /// <param name="Token">The full <c>AIKI-</c> token string.</param>
    public record VerifyRequest(string Token);

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
