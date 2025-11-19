// API Contract: Batch Size Exceeded Exception
// Feature: 001-onepassword-sdk

namespace OnePassword.Sdk.Exceptions;

/// <summary>
/// Thrown when batch retrieval exceeds the maximum allowed secrets limit.
/// </summary>
/// <remarks>
/// Corresponds to FR-025: batch size exceeded exception type
/// Corresponds to FR-022, FR-028: enforce maximum of 100 secrets per batch
///
/// Error message format: "Batch secret retrieval limit exceeded: {count} secrets requested,
/// maximum is 100"
/// </remarks>
public class BatchSizeExceededException : OnePasswordException
{
    /// <summary>
    /// Gets the number of secrets that were requested.
    /// </summary>
    public int RequestedCount { get; }

    /// <summary>
    /// Gets the maximum number of secrets allowed per batch.
    /// </summary>
    public int MaximumAllowed { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchSizeExceededException"/> class.
    /// </summary>
    /// <param name="requestedCount">The number of secrets requested.</param>
    /// <param name="maximumAllowed">The maximum number of secrets allowed.</param>
    public BatchSizeExceededException(int requestedCount, int maximumAllowed = 100)
        : base($"Batch secret retrieval limit exceeded: {requestedCount} secrets requested, maximum is {maximumAllowed}")
    {
        RequestedCount = requestedCount;
        MaximumAllowed = maximumAllowed;
    }
}
