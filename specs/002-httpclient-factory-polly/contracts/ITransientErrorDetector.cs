// Contract: ITransientErrorDetector
// Feature: 002-httpclient-factory-polly
// Purpose: Interface for classifying HTTP errors as transient (retryable) or permanent (FR-005, FR-010)

using System.Net;

namespace OnePassword.Sdk.Resilience;

/// <summary>
/// Classifies HTTP errors as transient (retryable) or permanent (non-retryable).
/// </summary>
/// <remarks>
/// Implements FR-005 (distinguish transient from permanent errors) and FR-010
/// (do not retry auth failures, authorization failures, or not found errors).
/// </remarks>
public interface ITransientErrorDetector
{
    /// <summary>
    /// Determines if an HTTP status code represents a transient error that should be retried.
    /// </summary>
    /// <param name="statusCode">HTTP response status code</param>
    /// <returns>True if the error is transient and should be retried; otherwise false</returns>
    /// <remarks>
    /// Transient errors (retryable):
    /// - 408 Request Timeout
    /// - 429 Too Many Requests
    /// - 500 Internal Server Error
    /// - 502 Bad Gateway
    /// - 503 Service Unavailable
    /// - 504 Gateway Timeout
    ///
    /// Permanent errors (non-retryable - FR-010):
    /// - 401 Unauthorized (authentication failure)
    /// - 403 Forbidden (authorization failure)
    /// - 404 Not Found
    /// - All other 4xx client errors
    /// </remarks>
    bool IsTransientError(HttpStatusCode statusCode);

    /// <summary>
    /// Determines if an HTTP status code represents a permanent error that should not be retried.
    /// </summary>
    /// <param name="statusCode">HTTP response status code</param>
    /// <returns>True if the error is permanent and should not be retried; otherwise false</returns>
    /// <remarks>
    /// Permanent errors include 401 (Unauthorized), 403 (Forbidden), and 404 (Not Found) per FR-010.
    /// </remarks>
    bool IsPermanentError(HttpStatusCode statusCode);

    /// <summary>
    /// Determines if an exception represents a transient error that should be retried.
    /// </summary>
    /// <param name="exception">Exception to classify</param>
    /// <returns>True if the exception is transient and should be retried; otherwise false</returns>
    /// <remarks>
    /// Transient exceptions:
    /// - HttpRequestException with transient status code
    /// - TaskCanceledException due to timeout (not user cancellation)
    /// - SocketException (network issues)
    /// </remarks>
    bool IsTransientException(Exception exception);
}
