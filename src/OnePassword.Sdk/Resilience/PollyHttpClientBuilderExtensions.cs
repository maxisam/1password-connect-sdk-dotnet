// Resilience: Polly HttpClient Builder Extensions
// Feature: 002-httpclient-factory-polly

using Microsoft.Extensions.DependencyInjection;
using OnePassword.Sdk.Client;

namespace OnePassword.Sdk.Resilience;

/// <summary>
/// Extension methods for configuring Polly resilience policies on HttpClient via IHttpClientBuilder.
/// </summary>
/// <remarks>
/// Implements FR-040: Integration of ResiliencePolicyBuilder with IHttpClientFactory.
/// </remarks>
public static class PollyHttpClientBuilderExtensions
{
    /// <summary>
    /// Adds OnePassword-specific resilience policies (timeout, retry, circuit breaker) to the HttpClient.
    /// </summary>
    /// <param name="builder">The IHttpClientBuilder to configure.</param>
    /// <param name="options">The OnePasswordClientOptions containing resilience configuration.</param>
    /// <returns>The IHttpClientBuilder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when builder or options is null.</exception>
    /// <remarks>
    /// Configures the HttpClient with a complete resilience pipeline:
    /// - Timeout strategy (from options.Timeout)
    /// - Retry strategy with exponential backoff and jitter (from options.MaxRetries, RetryBaseDelay, etc.)
    /// - Circuit breaker strategy (from options.CircuitBreakerFailureThreshold, etc.)
    /// </remarks>
    public static IHttpClientBuilder AddOnePasswordResilience(
        this IHttpClientBuilder builder,
        OnePasswordClientOptions options)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        // Build and add the complete resilience pipeline
        var pipeline = ResiliencePolicyBuilder.BuildResiliencePipeline(options);

        // Add the pipeline as a delegating handler
        // Note: Don't set InnerHandler - IHttpClientFactory will set it up
        builder.AddHttpMessageHandler(() => new ResilienceHttpMessageHandler(pipeline, innerHandler: null));

        return builder;
    }
}
