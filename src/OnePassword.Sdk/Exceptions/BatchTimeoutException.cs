// API Contract: Batch Timeout Exception
// Feature: 001-onepassword-sdk

namespace OnePassword.Sdk.Exceptions;

/// <summary>
/// Thrown when batch retrieval operation exceeds the timeout limit.
/// </summary>
/// <remarks>
/// Corresponds to FR-025: timeout exception type
/// Corresponds to FR-024, FR-028: enforce 10-second total timeout for batch operations
///
/// Error message format: "Batch secret retrieval operation timed out after 10 seconds"
/// </remarks>
public class BatchTimeoutException : OnePasswordException
{
    /// <summary>
    /// Gets the timeout duration that was exceeded.
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchTimeoutException"/> class.
    /// </summary>
    /// <param name="timeout">The timeout duration that was exceeded.</param>
    public BatchTimeoutException(TimeSpan timeout)
        : base($"Batch secret retrieval operation timed out after {timeout.TotalSeconds} seconds")
    {
        Timeout = timeout;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchTimeoutException"/> class with an inner exception.
    /// </summary>
    /// <param name="timeout">The timeout duration that was exceeded.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public BatchTimeoutException(TimeSpan timeout, Exception innerException)
        : base($"Batch secret retrieval operation timed out after {timeout.TotalSeconds} seconds", innerException)
    {
        Timeout = timeout;
    }
}
