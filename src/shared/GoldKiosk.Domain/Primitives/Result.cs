namespace GoldKiosk.Domain.Primitives;

/// <summary>
/// The outcome of a domain operation: success, or failure with a <see cref="DomainError"/>.
/// Expected business failures (stale price, KYC pending, hardware busy) flow through results;
/// exceptions are reserved for exceptional states.
/// </summary>
public class Result
{
    private readonly DomainError? _error;

    private protected Result(bool isSuccess, DomainError? error)
    {
        if (isSuccess && error is not null)
        {
            throw new ArgumentException("A success result cannot carry an error.", nameof(error));
        }

        if (!isSuccess && error is null)
        {
            throw new ArgumentException("A failure result must carry an error.", nameof(error));
        }

        IsSuccess = isSuccess;
        _error = error;
    }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets a value indicating whether the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Gets the error of a failed result.</summary>
    /// <exception cref="InvalidOperationException">The result is a success.</exception>
    public DomainError Error =>
        _error ?? throw new InvalidOperationException("A success result has no error.");

    /// <summary>Creates a successful result.</summary>
    /// <returns>The success result.</returns>
    public static Result Success() => new(true, null);

    /// <summary>Creates a failed result.</summary>
    /// <param name="error">The domain error describing the failure.</param>
    /// <returns>The failure result.</returns>
    public static Result Failure(DomainError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(false, error);
    }

    /// <summary>Creates a successful result carrying a value.</summary>
    /// <typeparam name="T">The type of the carried value.</typeparam>
    /// <param name="value">The value produced by the operation.</param>
    /// <returns>The success result.</returns>
    public static Result<T> Success<T>(T value) => new(value, true, null);

    /// <summary>Creates a failed result of <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The type the operation would have produced.</typeparam>
    /// <param name="error">The domain error describing the failure.</param>
    /// <returns>The failure result.</returns>
    public static Result<T> Failure<T>(DomainError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<T>(default, false, error);
    }
}
