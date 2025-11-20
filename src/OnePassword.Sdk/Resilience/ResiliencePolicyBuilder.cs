// Resilience: Policy Builder
// Feature: 002-httpclient-factory-polly

using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using OnePassword.Sdk.Client;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace OnePassword.Sdk.Resilience;

/// <summary>
/// Builds Polly v8 resilience policies from OnePasswordClientOptions.
/// </summary>
/// <remarks>
/// Implements FR-003 (exponential backoff), FR-004 (circuit breaker), FR-014 (configurable thresholds), FR-015 (jitter).
/// </remarks>
public static class ResiliencePolicyBuilder
{
    /// <summary>
    /// Creates an HTTP retry strategy from client options.
    /// </summary>
    /// <param name="options">The client options containing retry configuration.</param>
    /// <returns>Configured HttpRetryStrategyOptions.</returns>
    public static HttpRetryStrategyOptions BuildRetryStrategy(OnePasswordClientOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        return new HttpRetryStrategyOptions
        {
            // FR-008: Maintain existing MaxRetries setting for backward compatibility
            MaxRetryAttempts = options.MaxRetries,

            // FR-003, FR-015: Exponential backoff with jitter
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = options.EnableJitter,
            Delay = options.RetryBaseDelay,
            MaxDelay = options.RetryMaxDelay,

            // FR-005, FR-010: Only retry on transient errors
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .HandleResult(response => TransientErrorDetector.IsTransient(response))
                .Handle<HttpRequestException>()
                .Handle<TimeoutException>(),

            // FR-009: Log retry attempts at Warning level
            OnRetry = args =>
            {
                var logger = options.Logger;
                if (logger != null && args.Outcome.Result != null)
                {
                    logger.LogWarning(
                        "Retry attempt {AttemptNumber} after {Delay}ms due to {StatusCode} response. Endpoint: {RequestUri}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Result.StatusCode,
                        args.Outcome.Result.RequestMessage?.RequestUri);
                }
                else if (logger != null && args.Outcome.Exception != null)
                {
                    logger.LogWarning(
                        args.Outcome.Exception,
                        "Retry attempt {AttemptNumber} after {Delay}ms due to exception",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds);
                }

                return ValueTask.CompletedTask;
            }
        };
    }

    /// <summary>
    /// Creates an HTTP circuit breaker strategy from client options.
    /// </summary>
    /// <param name="options">The client options containing circuit breaker configuration.</param>
    /// <returns>Configured HttpCircuitBreakerStrategyOptions.</returns>
    public static HttpCircuitBreakerStrategyOptions BuildCircuitBreakerStrategy(OnePasswordClientOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        return new HttpCircuitBreakerStrategyOptions
        {
            // FR-014: Configurable circuit breaker thresholds
            FailureRatio = 1.0, // Open after consecutive failures (not ratio-based)
            MinimumThroughput = options.CircuitBreakerFailureThreshold,
            SamplingDuration = options.CircuitBreakerSamplingDuration,
            BreakDuration = options.CircuitBreakerBreakDuration,

            // FR-005: Circuit breaker should react to transient errors
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .HandleResult(response => TransientErrorDetector.IsTransient(response))
                .Handle<HttpRequestException>()
                .Handle<TimeoutException>(),

            // FR-009: Log circuit state changes at Warning level
            OnOpened = args =>
            {
                var logger = options.Logger;
                logger?.LogWarning(
                    "Circuit breaker opened. Break duration: {BreakDuration}s",
                    args.BreakDuration.TotalSeconds);

                return ValueTask.CompletedTask;
            },
            OnClosed = args =>
            {
                var logger = options.Logger;
                logger?.LogWarning("Circuit breaker closed. Normal operation resumed");

                return ValueTask.CompletedTask;
            },
            OnHalfOpened = args =>
            {
                var logger = options.Logger;
                logger?.LogWarning("Circuit breaker half-open. Testing with probe request");

                return ValueTask.CompletedTask;
            }
        };
    }

    /// <summary>
    /// Creates an HTTP timeout strategy from client options.
    /// </summary>
    /// <param name="options">The client options containing timeout configuration.</param>
    /// <returns>Configured HttpTimeoutStrategyOptions.</returns>
    public static HttpTimeoutStrategyOptions BuildTimeoutStrategy(OnePasswordClientOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        return new HttpTimeoutStrategyOptions
        {
            // FR-017: Overall request timeout
            Timeout = options.Timeout,

            // FR-009: Log timeout events
            OnTimeout = args =>
            {
                var logger = options.Logger;
                logger?.LogWarning(
                    "Request timed out after {Timeout}s",
                    args.Timeout.TotalSeconds);

                return ValueTask.CompletedTask;
            }
        };
    }

    /// <summary>
    /// Builds a complete resilience pipeline combining timeout, retry, and circuit breaker strategies.
    /// </summary>
    /// <param name="options">The client options containing all resilience configuration.</param>
    /// <returns>Configured ResiliencePipeline for HTTP responses.</returns>
    /// <remarks>
    /// Pipeline execution order: Timeout → Retry → Circuit Breaker (innermost to outermost)
    /// This ensures timeouts apply to individual attempts, retries wrap them, and circuit breaker wraps everything.
    /// </remarks>
    public static ResiliencePipeline<HttpResponseMessage> BuildResiliencePipeline(OnePasswordClientOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            // Outermost: Circuit breaker - prevents cascading failures
            .AddCircuitBreaker(BuildCircuitBreakerStrategy(options))
            // Middle: Retry - handles transient failures with backoff
            .AddRetry(BuildRetryStrategy(options))
            // Innermost: Timeout - applies to each individual attempt
            .AddTimeout(BuildTimeoutStrategy(options))
            .Build();
    }
}
