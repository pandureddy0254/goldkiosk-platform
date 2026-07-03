using System.Text.Json;
using GoldKiosk.Cloud.AdminPortal.Logging;

namespace GoldKiosk.Cloud.AdminPortal.Services.Licensing;

/// <summary>
/// <see cref="ILicenseStore"/> backed by one JSON file per tenant under
/// <c>App_Data/licenses/</c> plus an <c>active.txt</c> pointer file.
/// </summary>
public sealed class FileLicenseStore : ILicenseStore
{
    private static readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly JsonSerializerOptions _writeOptions = new() { WriteIndented = true };

    private readonly string _root;
    private readonly ILogger<FileLicenseStore> _logger;

    /// <summary>
    /// Initializes the store rooted at <c>App_Data/licenses</c> under the content root.
    /// </summary>
    /// <param name="env">Host environment used to resolve the content root.</param>
    /// <param name="logger">Logger for corrupt-file diagnostics.</param>
    public FileLicenseStore(IHostEnvironment env, ILogger<FileLicenseStore> logger)
    {
        _root = Path.Combine(env.ContentRootPath, "App_Data", "licenses");
        _logger = logger;
        Directory.CreateDirectory(_root);
    }

    private string ActivePointerPath => Path.Combine(_root, "active.txt");

    private string TenantFile(Guid id) => Path.Combine(_root, id.ToString("D") + ".json");

    /// <inheritdoc />
    public async Task<StoredLicense?> GetActiveAsync(CancellationToken ct = default)
    {
        if (!File.Exists(ActivePointerPath))
        {
            return null;
        }

        var raw = await File.ReadAllTextAsync(ActivePointerPath, ct);
        if (!Guid.TryParse(raw.Trim(), out var id))
        {
            return null;
        }

        return await GetByTenantAsync(id, ct);
    }

    /// <inheritdoc />
    public async Task<StoredLicense?> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        var path = TenantFile(tenantId);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(path, ct);
            return JsonSerializer.Deserialize<StoredLicense>(json);
        }
        catch (JsonException ex)
        {
            _logger.StoredLicenseCorrupt(ex, path);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(StoredLicense entry, bool setActive, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await _lock.WaitAsync(ct);
        try
        {
            var path = TenantFile(entry.License.TenantId);
            var json = JsonSerializer.Serialize(entry, _writeOptions);
            await File.WriteAllTextAsync(path, json, ct);
            if (setActive)
            {
                await File.WriteAllTextAsync(ActivePointerPath, entry.License.TenantId.ToString("D"), ct);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task ClearActiveAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (File.Exists(ActivePointerPath))
            {
                File.Delete(ActivePointerPath);
            }
        }
        finally
        {
            _lock.Release();
        }
    }
}
