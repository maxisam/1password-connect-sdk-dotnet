// API Contract: Network Exception
// Feature: 001-onepassword-sdk

namespace OnePassword.Sdk.Exceptions;

/// <summary>
/// Thrown when network communication with 1Password Connect API fails after retries.
/// </summary>
/// <remarks>
/// Corresponds to FR-025: network error exception type
/// Corresponds to FR-033: retry transient network failures up to 3 times
///
/// This exception is thrown only AFTER retry attempts have been exhausted.
/// Transient failures are retried 3 times with exponential backoff (1s, 2s, 4s).
///
/// Typical causes:
/// - 1Password Connect server unreachable
/// - DNS resolution failure
/// - Network connectivity issues
/// - Firewall blocking connection
///
/// Resolution: Verify Connect server URL, check network connectivity, verify firewall rules.
/// </remarks>
public class NetworkException : OnePasswordException
{
    /// <summary>
    /// Gets the number of retry attempts that were made before this exception was thrown.
    /// </summary>
    public int RetryAttempts { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="retryAttempts">The number of retry attempts made.</param>
    public NetworkException(string message, int retryAttempts = 3)
        : base($"{message} (after {retryAttempts} retry attempts)")
    {
        RetryAttempts = retryAttempts;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <param name="retryAttempts">The number of retry attempts made.</param>
    public NetworkException(string message, Exception innerException, int retryAttempts = 3)
        : base($"{message} (after {retryAttempts} retry attempts)", innerException)
    {
        RetryAttempts = retryAttempts;
    }
}
