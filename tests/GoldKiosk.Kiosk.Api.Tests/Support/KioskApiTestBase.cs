using NUnit.Framework;

namespace GoldKiosk.Kiosk.Api.Tests.Support;

/// <summary>
/// Base plumbing for Kiosk API integration fixtures: a fresh factory (own temp store, own
/// fake clock, all-simulated devices) and client per test, disposed in teardown.
/// </summary>
public abstract class KioskApiTestBase
{
    protected KioskApiFactory Factory { get; private set; } = null!;

    protected HttpClient Client { get; private set; } = null!;

    /// <summary>Extra configuration keys applied on top of the test defaults.</summary>
    protected virtual IReadOnlyDictionary<string, string?> ConfigurationOverrides { get; } =
        new Dictionary<string, string?>();

    [SetUp]
    public void CreateHost()
    {
        Factory = new KioskApiFactory(ConfigurationOverrides);
        Client = Factory.CreateClient();
    }

    [TearDown]
    public void DisposeHost()
    {
        Client.Dispose();
        Factory.Dispose();
    }
}
