using System.Diagnostics;
using GoldKiosk.TestKit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace GoldKiosk.Kiosk.Api.Tests.Support;

/// <summary>
/// WebApplicationFactory for the Kiosk API host: every device runs its simulator (the
/// appsettings default), simulated latency is removed, the transaction store points at a
/// per-factory temp folder, and the host's <see cref="TimeProvider"/> is replaced with a
/// <see cref="FakeTimeProvider"/> pinned to <see cref="KioskClock.DefaultNow"/>.
/// </summary>
public sealed class KioskApiFactory : WebApplicationFactory<Program>
{
    private readonly IReadOnlyDictionary<string, string?> _extraSettings;

    public KioskApiFactory()
        : this(new Dictionary<string, string?>())
    {
    }

    public KioskApiFactory(IReadOnlyDictionary<string, string?> extraSettings)
    {
        _extraSettings = extraSettings;
        TransactionRoot = Path.Combine(Path.GetTempPath(), "goldkiosk-api-tests", Guid.NewGuid().ToString("N"));
    }

    /// <summary>The per-factory transaction store root on disk.</summary>
    public string TransactionRoot { get; }

    /// <summary>The fake clock injected into the host.</summary>
    public FakeTimeProvider TimeProvider { get; } = KioskClock.CreateTimeProvider();

    /// <summary>
    /// Polls the transaction store until a <c>journal.json</c> satisfying
    /// <paramref name="accept"/> exists, returning its text. Used to observe background
    /// pipelines (settlement writes the receipt into the journal).
    /// </summary>
    public async Task<string> WaitForJournalAsync(Func<string, bool> accept, string description)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < TimeSpan.FromSeconds(15))
        {
            string? text = await TryReadFirstJournalAsync();
            if (text is not null && accept(text))
            {
                return text;
            }

            await Task.Delay(25);
        }

        throw new TimeoutException($"No journal satisfying '{description}' appeared under {TransactionRoot}.");
    }

    /// <summary>Whether a legacy-shaped <c>transactionDetails.json</c> exists in the store.</summary>
    public bool HasTransactionDetailsFile() =>
        Directory.Exists(TransactionRoot)
        && Directory.EnumerateFiles(TransactionRoot, "transactionDetails.json", SearchOption.AllDirectories).Any();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Kiosk:TransactionRoot"] = TransactionRoot,
            ["Devices:Simulation:LatencyMultiplier"] = "0",
            // Keep the rolling file sink out of the repo tree and away from parallel hosts.
            ["Serilog:WriteTo:1:Args:path"] = Path.Combine(TransactionRoot, "logs", "kiosk-api-.log"),
        };
        foreach ((string key, string? value) in _extraSettings)
        {
            settings[key] = value;
        }

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(TimeProvider);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            if (Directory.Exists(TransactionRoot))
            {
                Directory.Delete(TransactionRoot, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup; a straggling handle must not fail the test run.
        }
        catch (UnauthorizedAccessException)
        {
            // Same: leftover temp folders are harmless.
        }
    }

    private async Task<string?> TryReadFirstJournalAsync()
    {
        if (!Directory.Exists(TransactionRoot))
        {
            return null;
        }

        string? journalPath = Directory
            .EnumerateFiles(TransactionRoot, "journal.json", SearchOption.AllDirectories)
            .FirstOrDefault();
        if (journalPath is null)
        {
            return null;
        }

        try
        {
            return await File.ReadAllTextAsync(journalPath);
        }
        catch (IOException)
        {
            // The store may be mid-replace; the next poll retries.
            return null;
        }
    }
}
