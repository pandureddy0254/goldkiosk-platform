namespace GoldKiosk.Domain.Primitives;

/// <summary>
/// The outcome of a domain operation that produces a value on success.
/// Create instances via <see cref="Result.Success{T}(T)"/> and <see cref="Result.Failure{T}(DomainError)"/>.
/// </summary>
/// <typeparam name="T">The type of the value produced on success.</typeparam>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, bool isSuccess, DomainError? error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>Gets the value of a successful result.</summary>
    /// <exception cref="InvalidOperationException">The result is a failure.</exception>
    public T Value => IsSuccess
        ? _value! // Safe by construction: success results are only created from a T via Result.Success<T>.
        : throw new InvalidOperationException("A failure result has no value.");
}
