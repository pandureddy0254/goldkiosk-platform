using System.Text.Json;
using FluentAssertions;
using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Kiosk.Core.Analysis;
using GoldKiosk.Kiosk.Core.Options;
using GoldKiosk.Kiosk.Core.Persistence;
using GoldKiosk.Kiosk.Core.Sessions;
using GoldKiosk.TestKit;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Core.Tests.Persistence;

[TestFixture]
public sealed class FileSessionStoreTests
{
    // KioskClock.DefaultNow (2026-07-02T14:00:00Z) rendered with the store's folder formats.
    private const string DayFolder = "02-07-2026";
    private const string TimeFolder = "14-00-00";

    private string _root = null!;
    private FakeTimeProvider _timeProvider = null!;
    private FileSessionStore _store = null!;

    [SetUp]
    public void CreateStore()
    {
        _root = Path.Combine(Path.GetTempPath(), "goldkiosk-tests", Guid.NewGuid().ToString("N"));
        _timeProvider = KioskClock.CreateTimeProvider();
        _store = new FileSessionStore(
            new KioskOptions
            {
                KioskId = "GK-TEST-01",
                StoreId = "STORE-T",
                TransactionRoot = _root,
                TermsVersion = SessionMother.TermsVersion,
            },
            _timeProvider);
    }

    [TearDown]
    public void DeleteRoot()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup; a straggling handle must not fail the test run.
        }
    }

    [Test]
    public async Task CreateTransactionFolderAsync_FirstSessionOfTheSecond_CreatesDayTimeFolderWithJournalAndLog()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.PlacingItem);

        await _store.CreateTransactionFolderAsync(session);

        session.TransactionFolder.Should().Be(Path.Combine(_root, DayFolder, TimeFolder));
        File.Exists(Path.Combine(session.TransactionFolder!, "journal.json")).Should().BeTrue();
        File.Exists(Path.Combine(session.TransactionFolder!, "transactionLog.txt")).Should().BeTrue();
    }

    [Test]
    public async Task CreateTransactionFolderAsync_SameSecondCollision_AppendsIncrementingSuffix()
    {
        TransactionSession first = SessionDriver.InState(SessionStates.PlacingItem);
        TransactionSession second = SessionDriver.InState(SessionStates.PlacingItem);
        TransactionSession third = SessionDriver.InState(SessionStates.PlacingItem);

        await _store.CreateTransactionFolderAsync(first);
        await _store.CreateTransactionFolderAsync(second);
        await _store.CreateTransactionFolderAsync(third);

        first.TransactionFolder.Should().Be(Path.Combine(_root, DayFolder, TimeFolder));
        second.TransactionFolder.Should().Be(Path.Combine(_root, DayFolder, $"{TimeFolder}-2"));
        third.TransactionFolder.Should().Be(Path.Combine(_root, DayFolder, $"{TimeFolder}-3"));
    }

    [Test]
    public async Task PersistAsync_WithoutTransactionFolder_IsANoOp()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.PlacingItem);

        await _store.PersistAsync(session);

        Directory.Exists(_root).Should().BeFalse();
    }

    [Test]
    public async Task PersistAsync_AfterTransition_RewritesJournalWithoutLeavingTempFiles()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.PlacingItem);
        await _store.CreateTransactionFolderAsync(session);
        session.CloseTray(hasItem: true);

        await _store.PersistAsync(session);

        string journalText = await File.ReadAllTextAsync(Path.Combine(session.TransactionFolder!, "journal.json"));
        SessionJournal? journal = JsonSerializer.Deserialize<SessionJournal>(journalText, JournalOptions());
        journal!.State.Should().Be(SessionStates.Analyzing);
        journal.SessionId.Should().Be(session.Id);
        Directory.EnumerateFiles(session.TransactionFolder!, "*.tmp").Should().BeEmpty();
    }

    [Test]
    public async Task PersistAsync_JournalOnDisk_IsSnakeCase()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.PlacingItem);

        await _store.CreateTransactionFolderAsync(session);

        string journalText = await File.ReadAllTextAsync(Path.Combine(session.TransactionFolder!, "journal.json"));
        journalText.Should().Contain("\"session_id\"");
        journalText.Should().Contain("\"item_held\"");
        journalText.Should().NotContain("\"SessionId\"");
    }

    [Test]
    public async Task PersistAsync_JournalText_ExcludesEveryRestrictedPiiField()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Payout);
        session.ConfirmPayout(SessionMother.BankPayout());
        await _store.CreateTransactionFolderAsync(session);

        await _store.PersistAsync(session);

        string journalText = await File.ReadAllTextAsync(Path.Combine(session.TransactionFolder!, "journal.json"));
        journalText.Should().NotContain(SessionMother.Email, "email is PII and never journaled");
        journalText.Should().NotContain(SessionMother.Phone, "phone is PII and never journaled");
        journalText.Should().NotContain("Jordan", "customer names are PII and never journaled");
        journalText.Should().NotContain("Avery", "customer names are PII and never journaled");
        journalText.Should().NotContain("000123456789", "bank account numbers are never journaled");
        journalText.Should().NotContain("071000013", "bank routing numbers are never journaled");
        journalText.Should().NotContain("1994", "the customer's date of birth is never journaled");
        journalText.Should().Contain("\"receipt_channels\"", "only the channel selection survives contact capture");
        journalText.Should().Contain("\"payout_method\": \"bank_transfer\"");
    }

    [Test]
    public async Task WriteTransactionDetailsAsync_SettledCashSale_WritesLegacyFieldNamesVerbatim()
    {
        TransactionSession session = await SettledCashSessionAsync();

        await _store.WriteTransactionDetailsAsync(session);

        string text = await DetailsTextAsync(session);
        text.Should().ContainAll(
            "\"StoreTransactionId\"", "\"DispenserId\"", "\"StoreId\"", "\"LicenseNo\"", "\"Customer\"",
            "\"FingerPrintHash\"", "\"IsRefund\"", "\"Items\"", "\"TotalPayout\"", "\"CashDetails\"",
            "\"IsPawn\"", "\"IsCrypto\"", "\"CryptoCurrencySelected\"", "\"Temperature\"", "\"Weight\"",
            "\"DWTWeight\"", "\"Karat\"", "\"MetalPercentage\"", "\"MarketPrice\"", "\"TierPricePerDWT\"",
            "\"UnitPricePerDWT\"", "\"Impurities\"", "\"OfferPrice\"", "\"OfferAccepted\"", "\"OfferId\"",
            "\"Payout\"", "\"MetalType\"", "\"BagNumber\"", "\"ImageProcessingDir\"", "\"OfferMadeOn\"",
            "\"CustomerRespondedToOfferOn\"", "\"Phone\"", "\"First\"", "\"Last\"", "\"DOB\"", "\"Email\"");
    }

    [Test]
    public async Task WriteTransactionDetailsAsync_SettledCashSale_MapsMeasurementsMoneyAndBillCounts()
    {
        TransactionSession session = await SettledCashSessionAsync();

        await _store.WriteTransactionDetailsAsync(session);

        using JsonDocument details = JsonDocument.Parse(await DetailsTextAsync(session));
        JsonElement root = details.RootElement;
        JsonElement item = root.GetProperty("Items")[0];
        root.GetProperty("DispenserId").GetString().Should().Be("GK-TEST-01");
        root.GetProperty("StoreId").GetString().Should().Be("STORE-T");
        root.GetProperty("LicenseNo").GetString().Should().BeEmpty("restricted PII is withheld");
        root.GetProperty("FingerPrintHash").GetString().Should().BeEmpty("biometric PII is withheld");
        root.GetProperty("TotalPayout").GetDecimal().Should().Be(865m);
        root.GetProperty("IsPawn").GetBoolean().Should().BeFalse();
        root.GetProperty("Customer").GetProperty("First").GetString().Should().Be("Jordan");
        root.GetProperty("Customer").GetProperty("DOB").GetString().Should().Be("1994-03-15");
        root.GetProperty("Customer").GetProperty("Phone").GetString().Should().Be(SessionMother.Phone);
        item.GetProperty("Weight").GetDecimal().Should().Be(12.4m);
        item.GetProperty("DWTWeight").GetDecimal().Should().Be(7.973m);
        item.GetProperty("Karat").GetDecimal().Should().Be(22.0m);
        item.GetProperty("MetalPercentage").GetDecimal().Should().Be(91.6m);
        item.GetProperty("OfferPrice").GetDecimal().Should().Be(865m);
        item.GetProperty("OfferAccepted").GetBoolean().Should().BeTrue();
        item.GetProperty("MetalType").GetString().Should().Be("Gold");
        item.GetProperty("BagNumber").GetString().Should().Be("BAG-140000");
        root.GetProperty("CashDetails").GetProperty("Hundred").GetInt32().Should().Be(8);
        root.GetProperty("CashDetails").GetProperty("Fifty").GetInt32().Should().Be(1);
        root.GetProperty("CashDetails").GetProperty("Ten").GetInt32().Should().Be(1);
        root.GetProperty("CashDetails").GetProperty("Five").GetInt32().Should().Be(1);
        root.GetProperty("CashDetails").GetProperty("Twenty").GetInt32().Should().Be(0);
    }

    [Test]
    public async Task WriteTransactionDetailsAsync_OfferNotAccepted_RecordsZeroPayout()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Offer);
        await _store.CreateTransactionFolderAsync(session);

        await _store.WriteTransactionDetailsAsync(session);

        using JsonDocument details = JsonDocument.Parse(await DetailsTextAsync(session));
        JsonElement item = details.RootElement.GetProperty("Items")[0];
        item.GetProperty("OfferAccepted").GetBoolean().Should().BeFalse();
        item.GetProperty("OfferPrice").GetDecimal().Should().Be(865m);
        item.GetProperty("Payout").GetDecimal().Should().Be(0m);
        details.RootElement.GetProperty("TotalPayout").GetDecimal().Should().Be(0m);
    }

    [Test]
    public async Task WriteTransactionDetailsAsync_NoAnalysisRecorded_LeavesMetalTypeEmpty()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Analyzing);
        await _store.CreateTransactionFolderAsync(session);

        await _store.WriteTransactionDetailsAsync(session);

        using JsonDocument details = JsonDocument.Parse(await DetailsTextAsync(session));
        details.RootElement.GetProperty("Items")[0].GetProperty("MetalType").GetString().Should().BeEmpty();
    }

    [Test]
    public async Task WriteTransactionDetailsAsync_SilverDominantComposition_RecordsSilverMetalType()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Analyzing);
        session.RecordAnalysis(new AnalysisReading(
            20m, GoldPercent: 5m, SilverPercent: 90m, GoldPlated: false,
            new Dictionary<string, decimal> { ["Ag"] = 90m, ["Au"] = 5m }, 1.9m, VolumeCalibrated: true));
        await _store.CreateTransactionFolderAsync(session);

        await _store.WriteTransactionDetailsAsync(session);

        using JsonDocument details = JsonDocument.Parse(await DetailsTextAsync(session));
        details.RootElement.GetProperty("Items")[0].GetProperty("MetalType").GetString().Should().Be("Silver");
    }

    [Test]
    public async Task WriteTransactionDetailsAsync_PawnService_FlagsIsPawn()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Offer, serviceType: "pawn");
        await _store.CreateTransactionFolderAsync(session);

        await _store.WriteTransactionDetailsAsync(session);

        using JsonDocument details = JsonDocument.Parse(await DetailsTextAsync(session));
        details.RootElement.GetProperty("IsPawn").GetBoolean().Should().BeTrue();
    }

    [Test]
    public async Task RecoverCurrentDayAsync_FindsNonTerminalJournalsFromTodayAndYesterdayOnly()
    {
        TransactionSession interrupted = SessionDriver.InState(SessionStates.PlacingItem);
        await _store.CreateTransactionFolderAsync(interrupted);
        TransactionSession finished = SessionDriver.InState(SessionStates.Done);
        await _store.CreateTransactionFolderAsync(finished);
        WriteTornJournal();
        Directory.CreateDirectory(Path.Combine(_root, DayFolder, "10-00-00"));
        _timeProvider.Advance(TimeSpan.FromDays(1));
        TransactionSession today = SessionDriver.InState(SessionStates.PlacingItem);
        await _store.CreateTransactionFolderAsync(today);

        IReadOnlyList<RecoveredSession> recovered = await _store.RecoverCurrentDayAsync();

        recovered.Select(r => r.Journal.SessionId)
            .Should().BeEquivalentTo([interrupted.Id, today.Id]);
    }

    [Test]
    public async Task RecoverCurrentDayAsync_IgnoresJournalsOlderThanYesterday()
    {
        TransactionSession interrupted = SessionDriver.InState(SessionStates.PlacingItem);
        await _store.CreateTransactionFolderAsync(interrupted);
        _timeProvider.Advance(TimeSpan.FromDays(2));

        IReadOnlyList<RecoveredSession> recovered = await _store.RecoverCurrentDayAsync();

        recovered.Should().BeEmpty();
    }

    [Test]
    public async Task SafeAbortAsync_MarksJournalTerminalWithFaultReasonAndAuditLine()
    {
        TransactionSession interrupted = SessionDriver.InState(SessionStates.PlacingItem);
        await _store.CreateTransactionFolderAsync(interrupted);
        IReadOnlyList<RecoveredSession> recovered = await _store.RecoverCurrentDayAsync();

        await _store.SafeAbortAsync(recovered.Single());

        string journalText = await File.ReadAllTextAsync(Path.Combine(interrupted.TransactionFolder!, "journal.json"));
        journalText.Should().Contain("\"state\": \"done\"");
        journalText.Should().Contain("\"abort_reason\": \"fault\"");
        string logText = await File.ReadAllTextAsync(Path.Combine(interrupted.TransactionFolder!, "transactionLog.txt"));
        logText.Should().Contain("safe-aborted at startup recovery");
        (await _store.RecoverCurrentDayAsync()).Should().BeEmpty();
    }

    [TestCase("../evil.png")]
    [TestCase(@"..\evil.png")]
    [TestCase("sub/evil.png")]
    [TestCase(@"sub\evil.png")]
    public async Task WriteArtifactAsync_PathTraversalFileName_IsRejected(string fileName)
    {
        TransactionSession session = SessionDriver.InState(SessionStates.PlacingItem);
        await _store.CreateTransactionFolderAsync(session);

        await _store.Awaiting(s => s.WriteArtifactAsync(session, fileName, [1, 2, 3]))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public async Task WriteArtifactAsync_PlainFileName_WritesTheBytes()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.PlacingItem);
        await _store.CreateTransactionFolderAsync(session);

        await _store.WriteArtifactAsync(session, "signImage.png", [7, 8, 9]);

        byte[] written = await File.ReadAllBytesAsync(Path.Combine(session.TransactionFolder!, "signImage.png"));
        written.Should().Equal(7, 8, 9);
    }

    [Test]
    public async Task WriteArtifactAsync_WithoutTransactionFolder_IsANoOp()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.PlacingItem);

        await _store.WriteArtifactAsync(session, "signImage.png", [7, 8, 9]);

        Directory.Exists(_root).Should().BeFalse();
    }

    [Test]
    public async Task AppendLogAsync_WritesTimestampedLine()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.PlacingItem);
        await _store.CreateTransactionFolderAsync(session);

        await _store.AppendLogAsync(session, "Scale reading accepted.");

        string logText = await File.ReadAllTextAsync(Path.Combine(session.TransactionFolder!, "transactionLog.txt"));
        logText.Should().Contain("Scale reading accepted.");
        logText.Should().Contain("[2026-07-02 14:00:00");
    }

    [Test]
    public async Task AppendLogAsync_BlankMessage_Throws()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.PlacingItem);

        await _store.Awaiting(s => s.AppendLogAsync(session, " "))
            .Should().ThrowAsync<ArgumentException>();
    }

    private static JsonSerializerOptions JournalOptions() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private async Task<TransactionSession> SettledCashSessionAsync()
    {
        TransactionSession session = SessionDriver.InState(SessionStates.Settling);
        session.RecordAnalysis(new AnalysisReading(
            12.4m, GoldPercent: 91.6m, SilverPercent: 0.4m, GoldPlated: false,
            new Dictionary<string, decimal> { ["Au"] = 91.6m, ["Ag"] = 0.4m, ["Cu"] = 8.0m },
            0.70m, VolumeCalibrated: true));
        session.AssignBagNumber("BAG-140000");
        await _store.CreateTransactionFolderAsync(session);
        return session;
    }

    private static async Task<string> DetailsTextAsync(TransactionSession session) =>
        await File.ReadAllTextAsync(Path.Combine(session.TransactionFolder!, "transactionDetails.json"));

    private void WriteTornJournal()
    {
        string folder = Path.Combine(_root, DayFolder, "09-00-00");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "journal.json"), "{ this is not valid json");
    }
}
