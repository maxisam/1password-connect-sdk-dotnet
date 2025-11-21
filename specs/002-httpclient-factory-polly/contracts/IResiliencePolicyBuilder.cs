// Contract: IResiliencePolicyBuilder
// Feature: 002-httpclient-factory-polly
// Purpose: Interface for building Polly resilience policies from OnePasswordClientOptions

using Microsoft.Extensions.Http.Resilience;

namespace OnePassword.Sdk.Resilience;

/// <summary>
/// Builds Polly v8 resilience policies from OnePasswordClientOptions configuration.
/// </summary>
public interface IResiliencePolicyBuilder
{
    /// <summary>
    /// Creates a resilience policy configuration from client options.
    /// </summary>
    /// <param name="options">Client configuration containing retry, timeout, and circuit breaker settings</param>
    /// <returns>Policy configuration for Polly resilience pipeline</returns>
    /// <exception cref="ArgumentNullException">Thrown when options is null</exception>
    /// <remarks>
    /// This method translates OnePasswordClientOptions properties into Polly v8 strategy options:
    /// - MaxRetries → HttpRetryStrategyOptions.MaxRetryAttempts
    /// - RetryBaseDelay + EnableJitter → Exponential backoff with jitter (FR-015)
    /// - CircuitBreaker* properties → HttpCircuitBreakerStrategyOptions (FR-014)
    /// - Timeout → HttpTimeoutStrategyOptions
    /// </remarks>
    ResiliencePolicyConfiguration Build(OnePasswordClientOptions options);
}

/// <summary>
/// Internal model representing computed Polly policy configuration.
/// </summary>
public class ResiliencePolicyConfiguration
{
    /// <summary>
    /// Retry strategy options with exponential backoff and jitter.
    /// </summary>
    public required HttpRetryStrategyOptions RetryOptions { get; init; }

    /// <summary>
    /// Circuit breaker strategy options with failure threshold and break duration.
    /// </summary>
    public required HttpCircuitBreakerStrategyOptions CircuitBreakerOptions { get; init; }

    /// <summary>
    /// Timeout strategy options for per-request timeout enforcement.
    /// </summary>
    public required HttpTimeoutStrategyOptions TimeoutOptions { get; init; }
}
