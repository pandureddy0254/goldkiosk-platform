namespace GoldKiosk.Cloud.AdminPortal.Services.Common;

/// <summary>Generic paged-result envelope used by every list-style service method.</summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Total,
    int PageSize,
    int PageNo)
{
    /// <summary>Ceiling.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);

    /// <summary>An empty page for the given paging window.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1000:Do not declare static members on generic types",
        Justification = "Idiomatic factory on a generic record; call sites always know T.")]
    public static PagedResult<T> Empty(int pageSize, int pageNo)
        => new(Array.Empty<T>(), 0, pageSize, pageNo);
}

/// <summary>Outcome envelope for write operations (Add/Edit/Delete).</summary>
public sealed record OperationResult(bool Success, IReadOnlyList<string> Errors)
{
    /// <summary>Ok.</summary>
    public static OperationResult Ok() => new(true, Array.Empty<string>());
    /// <summary>Fail.</summary>
    public static OperationResult Fail(params string[] errors) => new(false, errors);

    /// <summary>Join.</summary>
    public string ErrorSummary => Errors.Count == 0 ? "" : string.Join("; ", Errors);
}
