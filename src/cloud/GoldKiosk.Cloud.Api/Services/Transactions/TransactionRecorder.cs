using System.Globalization;
using GoldKiosk.Cloud.Api.Logging;
using GoldKiosk.Contracts.V1.Cloud.Transactions;
using GoldKiosk.Domain.ValueObjects;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Entities.Tx;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.Api.Services.Transactions;

/// <summary>
/// Records completed kiosk transactions (adapted from platform2's
/// <c>TransactionService</c>: placeholder customer + transaction + item rows), made
/// idempotent on the kiosk-generated transaction id: a replay — outbox retry or repeated
/// <c>Idempotency-Key</c> — returns the original record; the primary-key/unique-constraint
/// race is caught and resolved by re-reading, never by double-inserting.
/// </summary>
/// <param name="db">The platform database (RLS tenant context set by the interceptor).</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">The host logger.</param>
public sealed class TransactionRecorder(
    AppDbContext db,
    TimeProvider timeProvider,
    ILogger<TransactionRecorder> logger) : ITransactionRecorder
{
    /// <inheritdoc />
    public async Task<RecordTransactionResponse> RecordAsync(
        RecordTransactionRequest request,
        Guid kioskId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        RecordTransactionResponse? existing = await FindExistingAsync(request.TransactionId, cancellationToken);
        if (existing is not null)
        {
            logger.TransactionReplayed(request.TransactionId);
            return existing;
        }

        Infrastructure.Entities.Kiosk.Kiosk kiosk = await db.Kiosks
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.Id == kioskId, cancellationToken)
            ?? throw new InvalidOperationException($"Kiosk {kioskId} has no fleet record.");

        DateTimeOffset now = timeProvider.GetUtcNow();
        string dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        Money amount = Money.FromMinorUnits(request.Amount.AmountMinor, request.Amount.Currency);

        // Placeholder customer — PII is enriched by the KYC flow, never written here.
        var customerId = Guid.NewGuid();
        var customer = new Infrastructure.Entities.Customer.Customer
        {
            Id = customerId,
            TenantId = tenantId,
            CustomerCode = $"C-{dateStamp}-{customerId.ToString()[..6].ToUpperInvariant()}",
            GivenNameEnc = [],
            FamilyNameEnc = [],
            Status = "prospect",
            CreatedAt = now,
            UpdatedAt = now,
        };

        string transactionCode =
            $"GK-{dateStamp}-{request.TransactionId.ToString()[..8].ToUpperInvariant()}";
        var transaction = new Transaction
        {
            Id = request.TransactionId,
            TenantId = tenantId,
            KioskId = kioskId,
            StoreId = kiosk.StoreId,
            CustomerId = customerId,
            TransactionCode = transactionCode,
            Kind = MapKind(request.Kind),
            Status = "paid",
            TotalAmount = amount.Amount,
            Fees = 0m,
            NetPayout = amount.Amount,
            CurrencyCode = amount.CurrencyCode,
            Source = "kiosk",
            StartedAt = now,
            CompletedAt = now,
            OccurredAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var item = new TransactionItem
        {
            Id = Guid.NewGuid(),
            TransactionId = request.TransactionId,
            ItemIndex = 0,
            ItemType = request.Category.ToLowerInvariant(),
            Metal = request.Metal.ToLowerInvariant(),
            DeclaredKarat = request.Karat,
            BilledKarat = request.Karat,
            BilledWeightG = decimal.Round(request.WeightGrams, 4),
            Description = string.Create(
                CultureInfo.InvariantCulture,
                $"{request.Karat:0.#}K {request.Metal.ToLowerInvariant()} {request.Category.ToLowerInvariant()}"),
            CreatedAt = now,
        };

        db.Customers.Add(customer);
        db.Transactions.Add(transaction);
        db.TransactionItems.Add(item);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Concurrent replay lost the insert race (PK on id / unique transaction_code):
            // the first writer's record is authoritative — return it.
            db.ChangeTracker.Clear();
            RecordTransactionResponse? raced = await FindExistingAsync(request.TransactionId, cancellationToken);
            if (raced is null)
            {
                throw;
            }

            logger.TransactionReplayed(request.TransactionId);
            return raced;
        }

        logger.TransactionRecorded(request.TransactionId, transactionCode);
        return new RecordTransactionResponse(request.TransactionId, customerId, transactionCode);
    }

    private async Task<RecordTransactionResponse?> FindExistingAsync(
        Guid transactionId, CancellationToken cancellationToken) =>
        await db.Transactions
            .AsNoTracking()
            .Where(t => t.Id == transactionId)
            .Select(t => new RecordTransactionResponse(t.Id, t.CustomerId, t.TransactionCode))
            .FirstOrDefaultAsync(cancellationToken);

    private static string MapKind(string kind) => kind.ToLowerInvariant() switch
    {
        "pawn" => "pawn",
        _ => "precious_sale",
    };
}
