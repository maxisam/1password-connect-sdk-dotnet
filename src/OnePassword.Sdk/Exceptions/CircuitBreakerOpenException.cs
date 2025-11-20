// Exceptions: Circuit Breaker Open Exception
// Feature: 002-httpclient-factory-polly

namespace OnePassword.Sdk.Exceptions;

/// <summary>
/// Exception thrown when the circuit breaker is open and requests cannot be processed.
/// </summary>
/// <remarks>
/// Implements FR-016: During batch operations, this exception includes partial results
/// for successfully fetched items and details of unfetched items.
/// </remarks>
public class CircuitBreakerOpenException : Exception
{
    /// <summary>
    /// Gets the partial results that were successfully fetched before the circuit opened.
    /// </summary>
    /// <remarks>
    /// Used in batch operations (e.g., GetSecretsAsync) to return successfully fetched items
    /// when the circuit breaker opens mid-execution.
    /// </remarks>
    public object? PartialResults { get; }

    /// <summary>
    /// Gets the references (identifiers) of items that failed to be fetched due to circuit breaker opening.
    /// </summary>
    /// <remarks>
    /// Contains the list of item references that could not be processed because the circuit opened.
    /// Allows callers to identify which items need to be retried later.
    /// </remarks>
    public IReadOnlyList<string> FailedReferences { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerOpenException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public CircuitBreakerOpenException(string message)
        : base(message)
    {
        FailedReferences = Array.Empty<string>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerOpenException"/> class with partial results.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="partialResults">The partial results that were successfully fetched.</param>
    /// <param name="failedReferences">The references of items that failed to be fetched.</param>
    public CircuitBreakerOpenException(string message, object? partialResults, IReadOnlyList<string> failedReferences)
        : base(message)
    {
        PartialResults = partialResults;
        FailedReferences = failedReferences ?? Array.Empty<string>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerOpenException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public CircuitBreakerOpenException(string message, Exception innerException)
        : base(message, innerException)
    {
        FailedReferences = Array.Empty<string>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerOpenException"/> class with partial results and an inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="partialResults">The partial results that were successfully fetched.</param>
    /// <param name="failedReferences">The references of items that failed to be fetched.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public CircuitBreakerOpenException(string message, object? partialResults, IReadOnlyList<string> failedReferences, Exception innerException)
        : base(message, innerException)
    {
        PartialResults = partialResults;
        FailedReferences = failedReferences ?? Array.Empty<string>();
    }
}
