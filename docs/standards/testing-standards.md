# GoldKiosk Testing Standards — NUnit + Moq + FluentAssertions

**The only permitted test stack.** xUnit, MSTest, NSubstitute, Shouldly are prohibited; if any
appear in generated code, that is a review blocker.

## Stack
- **NUnit** (latest 4.x) + `NUnit3TestAdapter` + `Microsoft.NET.Test.Sdk`
- **Moq** for test doubles
- **FluentAssertions** for all assertions (`result.Should().Be(...)`) — no `Assert.*` except `Assert.Throws`-style via FluentAssertions `Invoking`/`Awaiting`
- **Bogus** for fake data; **Testcontainers.PostgreSql** + `WebApplicationFactory` for integration tests; **Verify.NUnit** optional for contract snapshots
- Coverage: coverlet.collector; agents never execute tests — the user runs them in Visual Studio / CI.

## Structure
- `tests/<Project>.Tests` mirrors `src/.../<Project>` namespaces exactly; `GoldKiosk.IntegrationTests` for end-to-end API tests; shared builders/object mothers/fakes in `GoldKiosk.TestKit`.
- One fixture class per SUT class (`MoneyTests`, `CreateTransactionHandlerTests`); nested fixtures per method only when a class grows large.

## NUnit conventions
- `[TestFixture]` on fixtures; `[Test]` for single cases; `[TestCase(...)]` / `[TestCaseSource(nameof(Cases))]` for parameterized — never loops inside tests.
- `[SetUp]` builds fresh SUT + mocks per test; no shared mutable state between tests; `[OneTimeSetUp]` only for expensive immutable resources (e.g., Testcontainers database).
- No `[Order]`, no dependence on execution order, `[Parallelizable(ParallelScope.All)]` at assembly level where safe.
- Async tests return `Task`; never `async void`; never `.Result`.

## Naming & shape
- `Method_Scenario_ExpectedOutcome`: `SettleTransaction_WhenAlreadySettled_IsIdempotent`,
  `CreateTransaction_WhenGoldPriceStale_ReturnsPriceExpiredProblem`.
- AAA with blank-line separation, no `// Arrange` comments; one behavior per test.

## Moq conventions
- `new Mock<IPort>(MockBehavior.Strict)` for Application-layer ports — strict by default so unexpected calls fail; `Loose` only with a comment saying why.
- `mock.Setup(x => x.GetSpotPriceAsync(It.IsAny<CancellationToken>())).ReturnsAsync(price);`
- Verify observable outcomes; `mock.Verify(...)` only for interactions that ARE the behavior (event published, outbox enqueued) — not for incidental calls.
- Never mock types you don't own beyond port interfaces; fake `HttpMessageHandler` for HTTP clients; `FakeTimeProvider` for time.

## FluentAssertions conventions
- `actual.Should().Be(expected)`, `.BeEquivalentTo()` for DTO graphs (with explicit `options` when excluding ids/timestamps), `.Throw<DomainException>().WithMessage("*stale*")` via `Invoking`/`Awaiting`.
- Assert ProblemDetails by `type` code, not message text.

## What to test, by layer
| Layer | Test type | Doubles |
|---|---|---|
| Domain (+ Kiosk.Core) | Pure unit | None — needing Moq in a Domain test means the design is wrong |
| Application | Unit | Moq for ports only |
| Infrastructure | Integration | Real Postgres (Testcontainers); stub HTTP handlers for GoldAPI/Claude/Blob |
| Cloud.Api / Kiosk.Api | Integration | WebApplicationFactory, real pipeline, test auth handler |
| Kiosk.Devices | Contract tests | Simulators must pass the same contract fixture as real adapters |

Do not test: framework wiring, auto-properties, logic-free mapping.

## Financial-domain specifics
- Money paths: zero, negative, rounding policy boundaries, currency mismatch — every path.
- Gold weight/purity conversions against known reference values.
- Idempotency: replaying a command/idempotency key never double-charges or double-dispenses.
- Staleness: expired gold price blocks buy/sell with the specific ProblemDetails type.
- Outbox: crash-between-write-and-forward scenarios; at-least-once delivery with idempotent consumer.
- Concurrency: optimistic-conflict on settlement surfaces retry-or-conflict, never silent overwrite.

## Determinism
- `FakeTimeProvider`; seed Bogus (`Randomizer.Seed = new Random(8675309)`); no `Task.Delay` waits; no shared static state.

## Coverage policy
- New/changed code ≥ 80% branch; Domain value objects + pricing/settlement/outbox = 100%.
- Agents verify compilation with `dotnet build` only; NEVER run `dotnet test` — the user runs the suite in Visual Studio.
