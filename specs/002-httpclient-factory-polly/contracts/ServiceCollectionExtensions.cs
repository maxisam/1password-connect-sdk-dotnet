// Contract: ServiceCollectionExtensions
// Feature: 002-httpclient-factory-polly
// Purpose: Dependency injection registration for OnePasswordClient with resilience policies (FR-012, SC-005)

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace OnePassword.Sdk.Extensions;

/// <summary>
/// Extension methods for registering OnePasswordClient with IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers OnePasswordClient with dependency injection, including HttpClientFactory and resilience policies.
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    /// <param name="configureOptions">Action to configure OnePasswordClientOptions</param>
    /// <returns>IServiceCollection for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or configureOptions is null</exception>
    /// <remarks>
    /// This method (FR-012, SC-005):
    /// 1. Registers IOnePasswordClient as a singleton or scoped service
    /// 2. Configures a named HttpClient with IHttpClientFactory
    /// 3. Adds Polly resilience policies (retry, circuit breaker, timeout)
    /// 4. Validates OnePasswordClientOptions configuration (FR-018)
    ///
    /// Example usage:
    /// <code>
    /// services.AddOnePasswordClient(options =>
    /// {
    ///     options.ConnectServer = "https://connect.1password.com";
    ///     options.Token = configuration["OnePassword:Token"];
    ///     options.MaxRetries = 5;
    ///     options.CircuitBreakerFailureThreshold = 3;
    /// });
    /// </code>
    /// </remarks>
    public static IServiceCollection AddOnePasswordClient(
        this IServiceCollection services,
        Action<OnePasswordClientOptions> configureOptions);

    /// <summary>
    /// Registers OnePasswordClient with dependency injection using pre-configured options.
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    /// <param name="options">Pre-configured OnePasswordClientOptions</param>
    /// <returns>IServiceCollection for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or options is null</exception>
    /// <exception cref="ArgumentException">Thrown when options fail validation (FR-018)</exception>
    /// <remarks>
    /// This overload validates options immediately during service registration.
    /// Use the Action&lt;OnePasswordClientOptions&gt; overload for configuration binding scenarios.
    /// </remarks>
    public static IServiceCollection AddOnePasswordClient(
        this IServiceCollection services,
        OnePasswordClientOptions options);
}

/// <summary>
/// Extension methods for configuring HttpClient with OnePassword resilience policies.
/// </summary>
internal static class PollyHttpClientBuilderExtensions
{
    /// <summary>
    /// Adds OnePassword-specific resilience policies to an HttpClient builder.
    /// </summary>
    /// <param name="builder">HttpClient builder from AddHttpClient</param>
    /// <param name="options">OnePasswordClientOptions containing policy configuration</param>
    /// <returns>IHttpClientBuilder for method chaining</returns>
    /// <remarks>
    /// Configures the resilience pipeline in this order (strategies execute in sequence):
    /// 1. Timeout (per-request timeout enforcement)
    /// 2. Retry (exponential backoff with jitter - FR-003, FR-015)
    /// 3. Circuit Breaker (FR-004, FR-013, FR-014)
    ///
    /// Logging (FR-009):
    /// - Retry attempts: Warning level
    /// - Circuit breaker state changes: Warning level
    /// - Successful operations: Information level
    /// - Policy execution details: Debug level
    /// </remarks>
    internal static IHttpClientBuilder AddOnePasswordResilience(
        this IHttpClientBuilder builder,
        OnePasswordClientOptions options);
}
