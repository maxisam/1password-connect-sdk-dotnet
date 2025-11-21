// Resilience: Transient Error Detection
// Feature: 002-httpclient-factory-polly

using System.Net;

namespace OnePassword.Sdk.Resilience;

/// <summary>
/// Determines whether an HTTP error is transient (retryable) or permanent (non-retryable).
/// </summary>
/// <remarks>
/// Implements FR-005 and FR-010: Distinguish between transient and permanent errors.
/// Transient errors (408, 429, 500, 502, 503, 504) are retryable.
/// Permanent errors (401, 403, 404) bypass retry logic.
/// </remarks>
public static class TransientErrorDetector
{
    /// <summary>
    /// HTTP status codes considered transient (temporary failures that may succeed on retry).
    /// </summary>
    private static readonly HashSet<HttpStatusCode> TransientStatusCodes = new()
    {
        HttpStatusCode.RequestTimeout,          // 408
        HttpStatusCode.TooManyRequests,         // 429
        HttpStatusCode.InternalServerError,     // 500
        HttpStatusCode.BadGateway,              // 502
        HttpStatusCode.ServiceUnavailable,      // 503
        HttpStatusCode.GatewayTimeout           // 504
    };

    /// <summary>
    /// HTTP status codes considered permanent (non-retryable failures).
    /// </summary>
    private static readonly HashSet<HttpStatusCode> PermanentStatusCodes = new()
    {
        HttpStatusCode.Unauthorized,            // 401 - Authentication failure
        HttpStatusCode.Forbidden,               // 403 - Authorization failure
        HttpStatusCode.NotFound                 // 404 - Resource not found
    };

    /// <summary>
    /// Determines if an HTTP status code represents a transient error.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to check.</param>
    /// <returns>True if the error is transient and should be retried; otherwise false.</returns>
    public static bool IsTransient(HttpStatusCode statusCode)
    {
        return TransientStatusCodes.Contains(statusCode);
    }

    /// <summary>
    /// Determines if an HTTP status code represents a permanent error.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to check.</param>
    /// <returns>True if the error is permanent and should not be retried; otherwise false.</returns>
    public static bool IsPermanent(HttpStatusCode statusCode)
    {
        return PermanentStatusCodes.Contains(statusCode);
    }

    /// <summary>
    /// Determines if an HttpResponseMessage indicates a transient error.
    /// </summary>
    /// <param name="response">The HTTP response message to check.</param>
    /// <returns>True if the error is transient and should be retried; otherwise false.</returns>
    public static bool IsTransient(HttpResponseMessage response)
    {
        if (response == null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        return IsTransient(response.StatusCode);
    }

    /// <summary>
    /// Determines if an HttpResponseMessage indicates a permanent error.
    /// </summary>
    /// <param name="response">The HTTP response message to check.</param>
    /// <returns>True if the error is permanent and should not be retried; otherwise false.</returns>
    public static bool IsPermanent(HttpResponseMessage response)
    {
        if (response == null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        return IsPermanent(response.StatusCode);
    }
}
