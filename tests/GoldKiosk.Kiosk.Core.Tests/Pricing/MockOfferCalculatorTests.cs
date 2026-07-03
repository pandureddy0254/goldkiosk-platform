using FluentAssertions;
using GoldKiosk.Contracts.V1.Offers;
using GoldKiosk.Kiosk.Core.Options;
using GoldKiosk.Kiosk.Core.Pricing;
using GoldKiosk.TestKit;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Core.Tests.Pricing;

[TestFixture]
public sealed class MockOfferCalculatorTests
{
    private FakeTimeProvider _timeProvider = null!;
    private MockRatesOptions _rates = null!;
    private KioskOptions _kiosk = null!;
    private MockOfferCalculator _calculator = null!;

    [SetUp]
    public void CreateCalculator()
    {
        _timeProvider = KioskClock.CreateTimeProvider();
        _rates = new MockRatesOptions();
        _kiosk = new KioskOptions { OfferTtlSeconds = 600 };
        _calculator = new MockOfferCalculator(_rates, _kiosk, _timeProvider);
    }

    [Test]
    public async Task CalculateAsync_SaleWithDefaultRates_FloorsMeltValueTimesMarginToNearestFive()
    {
        var input = new OfferInput("sale", 12.4m, 91.6m);

        OfferDto offer = await _calculator.CalculateAsync(input);

        // melt = 12.4 g × 108.94 $/g × 0.916 = 1,237.384096; × 70% margin = 866.1688672;
        // banker's-rounded to $866.17, floored to the nearest $5 = $865.00.
        offer.Amount.AmountMinor.Should().Be(86_500);
        offer.Amount.Currency.Should().Be("USD");
        offer.Amount.Display.Should().Be("$865.00");
        offer.Kind.Should().Be("sale");
        offer.PawnTerms.Should().BeNull();
    }

    [Test]
    public async Task CalculateAsync_PureGoldHundredGrams_DropsSubFiveRemainderInHouseFavor()
    {
        var input = new OfferInput("sale", 100m, 100m);

        OfferDto offer = await _calculator.CalculateAsync(input);

        // melt = 100 × 108.94 × 1.00 = 10,894.00; × 70% = 7,625.80; floor-5 = 7,625.00.
        offer.Amount.AmountMinor.Should().Be(762_500);
        offer.Amount.Display.Should().Be("$7,625.00");
    }

    [Test]
    public async Task CalculateAsync_AnySaleAmount_IsAlwaysAWholeMultipleOfFiveMajorUnits()
    {
        var input = new OfferInput("sale", 3.7m, 58.5m);

        OfferDto offer = await _calculator.CalculateAsync(input);

        (offer.Amount.AmountMinor % 500).Should().Be(0);
    }

    [Test]
    public async Task CalculateAsync_Sale_LocksThePriceForTheConfiguredTtl()
    {
        var input = new OfferInput("sale", 12.4m, 91.6m);

        OfferDto offer = await _calculator.CalculateAsync(input);

        offer.ExpiresAt.Should().Be(KioskClock.DefaultNow.AddSeconds(600));
        offer.Verified.Should().BeTrue();
        offer.LivePrice.Should().BeTrue();
        offer.OfferId.Should().StartWith("off_");
    }

    [Test]
    public async Task CalculateAsync_Pawn_DerivesFeeTotalAndDueDateFromTheOffer()
    {
        var input = new OfferInput("pawn", 12.4m, 91.6m);

        OfferDto offer = await _calculator.CalculateAsync(input);

        // Offer $865.00; monthly fee = 5% of 865 = $43.25; total repayment =
        // 865 + 43.25 + 43.25 = $951.50; due 30 days after 2026-07-02; APR = 12 × 5% = 60%.
        offer.Kind.Should().Be("pawn");
        offer.Amount.AmountMinor.Should().Be(86_500);
        offer.PawnTerms.Should().NotBeNull();
        offer.PawnTerms!.MonthlyFee.AmountMinor.Should().Be(4_325);
        offer.PawnTerms.MonthlyFee.Display.Should().Be("$43.25");
        offer.PawnTerms.TotalRepayment.AmountMinor.Should().Be(95_150);
        offer.PawnTerms.TotalRepayment.Display.Should().Be("$951.50");
        offer.PawnTerms.AprPercent.Should().Be(60m);
        offer.PawnTerms.DueDate.Should().Be(new DateOnly(2026, 8, 1));
    }

    [Test]
    public async Task CalculateAsync_UnknownKind_FallsBackToSale()
    {
        var input = new OfferInput("barter", 12.4m, 91.6m);

        OfferDto offer = await _calculator.CalculateAsync(input);

        offer.Kind.Should().Be("sale");
        offer.PawnTerms.Should().BeNull();
    }

    [TestCase("0")]
    [TestCase("-1")]
    public async Task CalculateAsync_NonPositiveWeight_Throws(string weight)
    {
        var input = new OfferInput("sale", decimal.Parse(weight, System.Globalization.CultureInfo.InvariantCulture), 91.6m);

        await _calculator.Awaiting(c => c.CalculateAsync(input))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [TestCase("0")]
    [TestCase("-5")]
    public async Task CalculateAsync_NonPositiveGoldPercent_Throws(string goldPercent)
    {
        var input = new OfferInput("sale", 12.4m, decimal.Parse(goldPercent, System.Globalization.CultureInfo.InvariantCulture));

        await _calculator.Awaiting(c => c.CalculateAsync(input))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task CalculateAsync_NullInput_Throws()
    {
        // Null-forgiving: deliberately passing null to exercise the guard clause.
        await _calculator.Awaiting(c => c.CalculateAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public async Task CalculateAsync_CancelledToken_Throws()
    {
        var input = new OfferInput("sale", 12.4m, 91.6m);
        var cancelled = new CancellationToken(canceled: true);

        await _calculator.Awaiting(c => c.CalculateAsync(input, cancelled))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public void Constructor_NullRates_Throws()
    {
        // Null-forgiving: deliberately passing null to exercise the guard clause.
        FluentActions.Invoking(() => new MockOfferCalculator(null!, _kiosk, _timeProvider))
            .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Constructor_NullKioskOptions_Throws()
    {
        // Null-forgiving: deliberately passing null to exercise the guard clause.
        FluentActions.Invoking(() => new MockOfferCalculator(_rates, null!, _timeProvider))
            .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Constructor_NullTimeProvider_Throws()
    {
        // Null-forgiving: deliberately passing null to exercise the guard clause.
        FluentActions.Invoking(() => new MockOfferCalculator(_rates, _kiosk, null!))
            .Should().Throw<ArgumentNullException>();
    }
}
