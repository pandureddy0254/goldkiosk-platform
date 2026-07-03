# GoldKiosk C# Coding Standards (C# 14 / .NET 10)

Binding for all agents and humans. Enforced by `Directory.Build.props`
(`TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended`, nullable enabled),
`.editorconfig`, and `dotnet format`.

## Language & style
- `net10.0` everywhere; `net10.0-windows` only for Kiosk.UI, Kiosk.Devices, Kiosk.Diagnostics, Kiosk.Package. `LangVersion=latest`.
- File-scoped namespaces, implicit usings, nullable reference types everywhere; never suppress with `!` without a justification comment.
- `var` when the type is apparent from the right side; explicit type otherwise.
- Records for DTOs/contracts and immutable value carriers; classes for entities with behavior.
- Primary constructors for simple DI targets; readonly fields `_camelCase`.
- Pattern matching over type-check-and-cast; switch expressions over if-chains for mapping.
- Collection expressions (`[]`, `[..items]`); public surfaces expose `IReadOnlyList<T>`/`IReadOnlyCollection<T>`, never `List<T>`.
- `required` + `init` instead of constructor telescoping for options/DTOs.
- One top-level type per file; file name = type name; namespaces mirror project + folder path.

## Naming
- PascalCase: types, methods, properties, constants. camelCase: locals, parameters.
- Interfaces `I` prefix; async methods end in `Async`.
- No abbreviations except industry-standard (Id, Api, Db, Kyc, Crm, Msix, Ui in project names).
- Booleans read as predicates: `IsExpired`, `HasPendingSettlement`, `CanDispense`.
- Projects follow `GoldKiosk.<Tier>.<Component>` per CLAUDE.md Â§2 â€” never invent new names.

## Async & concurrency
- `async`/`await` all the way; never `.Result`, `.Wait()`, `GetAwaiter().GetResult()`.
- `async void` only for UI event handlers where the framework demands it (Blazor/WPF shell), with try/catch inside.
- Accept and propagate `CancellationToken` on every I/O path; `CancellationToken cancellationToken = default` on public async APIs.
- `ConfigureAwait(false)` in libraries (Domain/Application/Infrastructure/Kiosk.Core/Devices), not in hosts/UI.
- `ValueTask` only on measured hot paths.

## Errors & results
- Exceptions for exceptional states; `Result<T>`/domain-error pattern for expected business failures (stale price, KYC pending, insufficient purity, hardware busy).
- Never `catch (Exception)` without logging + specific handling or rethrow.
- API errors: RFC 7807 ProblemDetails with stable machine-readable `type` codes shared via Contracts.
- Guard clauses: `ArgumentNullException.ThrowIfNull`, `ArgumentOutOfRangeException.ThrowIfNegative`.

## Money, weight, time
- Money: `decimal` inside a `Money` value object (amount + currency, INR default). Never float/double. Rounding policy defined once in Domain and tested.
- Gold: `decimal` grams + `Purity` (karat/fineness) value object; conversions only in Domain.
- Time: injected `TimeProvider` â€” never `DateTime.Now`/`UtcNow` directly. Store UTC; display IST at the edge.

## Logging & observability
- Serilog via `ILogger<T>` with message templates â€” `logger.LogInformation("Transaction {TransactionId} settled for {Amount}", id, amount)`; never interpolation in templates.
- `LoggerMessage` source generators on hot paths.
- No PII (Aadhaar, PAN, phone, photos) and no secrets in logs, traces, or metric tags â€” log IDs.
- Cloud-bound telemetry only through the whitelist in configuration-and-operations Â§Telemetry.

## EF Core / PostgreSQL
- snake_case naming convention + Npgsql legacy-timestamp switch: global, immutable, switch at the very top of Program.cs.
- Mapping via `IEntityTypeConfiguration<T>`, one class per entity; no annotations, no config in `OnModelCreating` bodies.
- Read paths: `AsNoTracking()` + projection to DTOs; page every unbounded list.
- No lazy loading; explicit `Include` only for aggregate needs.
- Migrations additive, named `yyyyMMdd_Description`; never edit an applied migration.
- Raw SQL only via parameterized `FromSql`/`ExecuteSql` interpolated handlers.
- RLS: every tenant-scoped query path sets `app.tenant_id` GUC through the established interceptor â€” never bypass.

## API design (Minimal APIs)
- Versioned route groups `/api/v1/...`; endpoint classes per feature with a `MapEndpoints` convention.
- `TypedResults` + declared `Produces` metadata; validation via endpoint filters + FluentValidation.
- DTOs live in Contracts; never expose Domain entities on the wire.
- Idempotency keys mandatory on kiosk transaction-creating endpoints; edgeâ†’cloud consumers idempotent.
- SignalR hubs (Kiosk.Api hardware events): strongly-typed hubs, contracts in GoldKiosk.Contracts.

## Dependency injection & options
- Constructor injection only; no service locator, no stateful static singletons.
- Per-project `AddXxx()` registration extensions in `DependencyInjection.cs`.
- Options pattern for all config sections: `IOptions<T>` + `ValidateDataAnnotations().ValidateOnStart()`; kiosk options bind from the layered pipeline (configuration-and-operations Â§Layering).

## Prohibited
- xUnit or any test framework other than NUnit (see testing standards).
- `dynamic` in business code; reflection hacks; `#pragma warning disable` without justification comment; regions to hide length; TODO without ticket reference; `DateTime.Now`; float/double money.

