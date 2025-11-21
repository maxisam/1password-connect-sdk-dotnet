// Configuration: OnePassword Client Options
// Feature: 001-onepassword-sdk

using Microsoft.Extensions.Logging;

namespace OnePassword.Sdk.Client;

/// <summary>
/// Configuration options for the 1Password Connect API client.
/// </summary>
/// <remarks>
/// These options can be loaded from configuration files (appsettings.json) or environment variables.
/// Environment variables take precedence over configuration file values.
/// </remarks>
public class OnePasswordClientOptions
{
    /// <summary>
    /// Gets or sets the Connect server URL (e.g., "https://localhost:8080").
    /// </summary>
    /// <remarks>
    /// Must be a valid HTTPS URL (FR-037). HTTP URLs are rejected for security.
    /// </remarks>
    public string ConnectServer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the access token for authentication.
    /// </summary>
    /// <remarks>
    /// Security: This value MUST NOT be logged or persisted in plaintext (FR-036, FR-038).
    /// Store in environment variables in production environments.
    /// </remarks>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP request timeout.
    /// </summary>
    /// <remarks>
    /// Default: 10 seconds (FR-024).
    /// Must be greater than zero.
    /// </remarks>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for transient failures.
    /// </summary>
    /// <remarks>
    /// Default: 3 retries with exponential backoff (FR-033, FR-008).
    /// Must be greater than or equal to 0.
    /// </remarks>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay for exponential backoff retry policy.
    /// </summary>
    /// <remarks>
    /// Default: 1 second (FR-003, FR-015).
    /// Used as the starting delay; actual delays increase exponentially with jitter.
    /// Must be greater than zero.
    /// </remarks>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the maximum delay cap for exponential backoff.
    /// </summary>
    /// <remarks>
    /// Default: 30 seconds.
    /// Prevents excessive wait times during high retry counts.
    /// Must be greater than or equal to RetryBaseDelay.
    /// </remarks>
    public TimeSpan RetryMaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets whether to enable jittered exponential backoff.
    /// </summary>
    /// <remarks>
    /// Default: true (FR-015).
    /// Jitter adds randomness to prevent thundering herd scenarios.
    /// </remarks>
    public bool EnableJitter { get; set; } = true;

    /// <summary>
    /// Gets or sets the consecutive failure threshold before circuit breaker opens.
    /// </summary>
    /// <remarks>
    /// Default: 5 consecutive failures (FR-014).
    /// Must be at least 1.
    /// </remarks>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets how long the circuit breaker stays open.
    /// </summary>
    /// <remarks>
    /// Default: 30 seconds (FR-014).
    /// After this duration, circuit transitions to half-open to test recovery.
    /// Must be greater than zero.
    /// </remarks>
    public TimeSpan CircuitBreakerBreakDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the window for tracking failure ratio.
    /// </summary>
    /// <remarks>
    /// Default: 60 seconds (FR-014).
    /// Circuit breaker tracks failures within this time window.
    /// Must be greater than zero.
    /// </remarks>
    public TimeSpan CircuitBreakerSamplingDuration { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets the logger instance for structured logging (optional).
    /// </summary>
    /// <remarks>
    /// If provided, the client will log all operations with correlation IDs (FR-039 through FR-044).
    /// Logs include: INFO for successful operations, WARN for retries, ERROR for failures.
    /// Security: Logs never contain secret values or tokens (FR-036, FR-038).
    /// </remarks>
    public ILogger<OnePasswordClient>? Logger { get; set; }

    /// <summary>
    /// Validates the options and throws an exception if any are invalid.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when options are invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectServer))
        {
            throw new ArgumentException("ConnectServer is required.", nameof(ConnectServer));
        }

        if (!ConnectServer.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("ConnectServer must use HTTPS (FR-037).", nameof(ConnectServer));
        }

        if (string.IsNullOrWhiteSpace(Token))
        {
            throw new ArgumentException("Token is required.", nameof(Token));
        }

        if (Timeout <= TimeSpan.Zero)
        {
            throw new ArgumentException("Timeout must be greater than zero.", nameof(Timeout));
        }

        if (MaxRetries < 0)
        {
            throw new ArgumentException("MaxRetries must be greater than or equal to zero.", nameof(MaxRetries));
        }

        if (RetryBaseDelay <= TimeSpan.Zero)
        {
            throw new ArgumentException("RetryBaseDelay must be greater than zero.", nameof(RetryBaseDelay));
        }

        if (RetryMaxDelay < RetryBaseDelay)
        {
            throw new ArgumentException("RetryMaxDelay must be greater than or equal to RetryBaseDelay.", nameof(RetryMaxDelay));
        }

        if (CircuitBreakerFailureThreshold < 1)
        {
            throw new ArgumentException("CircuitBreakerFailureThreshold must be at least 1.", nameof(CircuitBreakerFailureThreshold));
        }

        if (CircuitBreakerBreakDuration <= TimeSpan.Zero)
        {
            throw new ArgumentException("CircuitBreakerBreakDuration must be greater than zero.", nameof(CircuitBreakerBreakDuration));
        }

        if (CircuitBreakerSamplingDuration <= TimeSpan.Zero)
        {
            throw new ArgumentException("CircuitBreakerSamplingDuration must be greater than zero.", nameof(CircuitBreakerSamplingDuration));
        }
    }
}
