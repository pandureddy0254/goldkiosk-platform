# Tools — reference material (never compiled)

The legacy CRM excluded this folder from the build via a `<Compile Remove>` in its
csproj. The CRMPortal csproj is intentionally untouched by the migration ticket, so
the same effect is achieved by shipping the samples as `.cs.txt` — MSBuild never
sees them as sources.

| File | Purpose |
|---|---|
| `LicenseVerifierSample.cs.txt` | Drop-in Ed25519 verifier for the Admin Dashboard rebuild. Copy into that project (rename to `.cs`, remove the `#if` guard, add `NSec.Cryptography`) to verify `AIKI-` license tokens offline against the CRM's published JWKS (`/.well-known/jwks.json`). |

## Key generation

The legacy repo bootstrapped the Ed25519 keypair with a one-shot
`dotnet script` (`scripts/gen-license-keypair.csx`, not migrated). Equivalent C#:

```csharp
using NSec.Cryptography;
var key = Key.Create(SignatureAlgorithm.Ed25519,
    new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });
var priv = Convert.ToBase64String(key.Export(KeyBlobFormat.RawPrivateKey));
var pub  = Convert.ToBase64String(key.Export(KeyBlobFormat.RawPublicKey));
```

Store the private half **only** in user-secrets (dev) / Key Vault (prod) under
`License:Keys:<kid>:PrivateKeyBase64`. The public half is publishable and feeds
the JWKS endpoint. Never commit either alongside a real `ActiveKid`.
