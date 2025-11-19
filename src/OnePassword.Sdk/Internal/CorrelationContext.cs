// Internal: Correlation ID tracking for request tracing
// Feature: 001-onepassword-sdk

using System.Diagnostics;

namespace OnePassword.Sdk.Internal;

/// <summary>
/// Provides correlation ID tracking for request tracing and observability (FR-044).
/// </summary>
/// <remarks>
/// Uses System.Diagnostics.Activity for distributed tracing support.
/// The correlation ID is automatically propagated through async call chains.
/// </remarks>
internal static class CorrelationContext
{
    private static readonly ActivitySource ActivitySource = new("OnePassword.Sdk", "1.0.0");

    /// <summary>
    /// Gets the current correlation ID for the active request.
    /// </summary>
    /// <returns>Correlation ID (Activity.Id or generated GUID)</returns>
    public static string GetCorrelationId()
    {
        return Activity.Current?.Id ?? Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Starts a new activity for tracing a specific operation.
    /// </summary>
    /// <param name="operationName">Name of the operation being traced</param>
    /// <returns>Activity instance (dispose when operation completes)</returns>
    public static Activity? StartActivity(string operationName)
    {
        return ActivitySource.StartActivity(operationName);
    }

    /// <summary>
    /// Adds a tag to the current activity for additional context.
    /// </summary>
    public static void AddTag(string key, string? value)
    {
        Activity.Current?.AddTag(key, value);
    }
}
