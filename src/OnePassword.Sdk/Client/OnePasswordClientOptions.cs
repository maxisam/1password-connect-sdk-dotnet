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
    /// Default: 3 retries with exponential backoff (FR-033).
    /// Must be greater than or equal to 0.
    /// </remarks>
    public int MaxRetries { get; set; } = 3;

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
    }
}
