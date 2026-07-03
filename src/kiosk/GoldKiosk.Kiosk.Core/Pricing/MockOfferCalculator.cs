using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Contracts.V1.Offers;
using GoldKiosk.Domain.ValueObjects;
using GoldKiosk.Kiosk.Core.Options;
using GoldKiosk.Kiosk.Core.Sessions;

namespace GoldKiosk.Kiosk.Core.Pricing;

/// <summary>
/// Deterministic offer pricing against the configured <see cref="MockRatesOptions"/> table:
/// melt value (weight × pure-gold rate × purity fraction) × store margin, floored to the
/// nearest 5 major units (the house-favorable legacy rounding). Pawn offers add the
/// repayment terms. Replaced by live cloud pricing behind <see cref="IOfferCalculator"/>.
/// </summary>
public sealed class MockOfferCalculator : IOfferCalculator
{
    private const decimal PawnMonthlyFeePercent = 5m;
    private const decimal PawnAprPercent = 60m;
    private const int PawnTermDays = 30;
    private const decimal OfferRoundingStep = 5m;

    private readonly MockRatesOptions _rates;
    private readonly KioskOptions _kiosk;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes the calculator.</summary>
    /// <param name="rates">The mock rate table and store margin.</param>
    /// <param name="kiosk">The kiosk options carrying the offer TTL.</param>
    /// <param name="timeProvider">Time source for the price-lock expiry.</param>
    public MockOfferCalculator(MockRatesOptions rates, KioskOptions kiosk, TimeProvider timeProvider)
    {
        _rates = rates ?? throw new ArgumentNullException(nameof(rates));
        _kiosk = kiosk ?? throw new ArgumentNullException(nameof(kiosk));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public Task<OfferDto> CalculateAsync(OfferInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(input.WeightGrams);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(input.GoldPercent);
        cancellationToken.ThrowIfCancellationRequested();

        decimal meltValue = input.WeightGrams * _rates.GoldPerGram * (input.GoldPercent / 100m);
        Money offerAmount = Money
            .From(meltValue * (_rates.StoreMarginPercent / 100m), _rates.Currency)
            .FloorToNearest(OfferRoundingStep);

        DateTimeOffset now = _timeProvider.GetUtcNow();
        DateTimeOffset expiresAt = now.AddSeconds(_kiosk.OfferTtlSeconds);
        bool isPawn = string.Equals(input.Kind, "pawn", StringComparison.Ordinal);

        var offer = new OfferDto(
            OfferId: KioskIdGenerator.NewOfferId(_timeProvider),
            Amount: ToMoneyDto(offerAmount),
            Kind: isPawn ? "pawn" : "sale",
            Verified: true,
            LivePrice: true,
            ExpiresAt: expiresAt,
            PawnTerms: isPawn ? BuildPawnTerms(offerAmount, now) : null);

        return Task.FromResult(offer);
    }

    private static PawnTermsDto BuildPawnTerms(Money offerAmount, DateTimeOffset now)
    {
        Money monthlyFee = Money.From(
            offerAmount.Amount * (PawnMonthlyFeePercent / 100m), offerAmount.CurrencyCode);
        Money totalRepayment = offerAmount + monthlyFee + monthlyFee;

        return new PawnTermsDto(
            MonthlyFee: ToMoneyDto(monthlyFee),
            AprPercent: PawnAprPercent,
            TotalRepayment: ToMoneyDto(totalRepayment),
            DueDate: DateOnly.FromDateTime(now.AddDays(PawnTermDays).UtcDateTime));
    }

    private static MoneyDto ToMoneyDto(Money money) =>
        new(money.ToMinorUnits(), money.CurrencyCode, money.ToString());
}
