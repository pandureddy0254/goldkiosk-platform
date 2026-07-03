using System.Net;

namespace GoldKiosk.Kiosk.UI.Services;

/// <summary>
/// Thrown when the Kiosk API returns a non-success status. Carries the parsed
/// RFC 7807 problem so the UI can route to the localized error catalogue by type code.
/// </summary>
public sealed class KioskApiException : Exception
{
    /// <summary>Initializes the exception.</summary>
    public KioskApiException()
        : base("The Kiosk API returned an error.")
    {
    }

    /// <summary>Initializes the exception with a message.</summary>
    /// <param name="message">The error message.</param>
    public KioskApiException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes the exception with a message and inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public KioskApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes the exception from an API problem response.</summary>
    /// <param name="statusCode">The HTTP status code returned.</param>
    /// <param name="problem">The parsed problem body, when one was present.</param>
    public KioskApiException(HttpStatusCode statusCode, KioskProblem? problem)
        : base($"Kiosk API returned {(int)statusCode} ({problem?.Code ?? "no problem body"}).")
    {
        StatusCode = statusCode;
        Problem = problem;
    }

    /// <summary>Gets the HTTP status code returned by the API.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>Gets the parsed RFC 7807 problem body, when one was present.</summary>
    public KioskProblem? Problem { get; }
}
