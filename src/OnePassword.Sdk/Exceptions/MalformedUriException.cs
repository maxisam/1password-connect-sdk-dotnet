// API Contract: Malformed URI Exception
// Feature: 001-onepassword-sdk

namespace OnePassword.Sdk.Exceptions;

/// <summary>
/// Thrown when an op:// URI has invalid syntax or missing components.
/// </summary>
/// <remarks>
/// Corresponds to FR-025: malformed URI exception type
/// Corresponds to FR-026, FR-028: URI validation before API calls
/// Corresponds to FR-029, FR-032: clear error messages for malformed URIs
///
/// This exception is thrown BEFORE any API calls are made (fail-fast validation).
///
/// Error message format (FR-029):
/// "Malformed op:// URI in configuration key '{configKey}': {reason}.
///  Expected format: op://&lt;vault&gt;/&lt;item&gt;/&lt;field&gt; or op://&lt;vault&gt;/&lt;item&gt;/&lt;section&gt;/&lt;field&gt;"
///
/// Common reasons:
/// - Missing op:// prefix
/// - Empty vault, item, or field components
/// - Invalid number of path segments
/// - Improperly URL-encoded components
/// </remarks>
public class MalformedUriException : OnePasswordException
{
    /// <summary>
    /// Gets the configuration key that contained the malformed URI.
    /// </summary>
    public string ConfigurationKey { get; }

    /// <summary>
    /// Gets the malformed URI string.
    /// </summary>
    public string MalformedUri { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MalformedUriException"/> class.
    /// </summary>
    /// <param name="configurationKey">The configuration key that contained the malformed URI.</param>
    /// <param name="malformedUri">The malformed URI string.</param>
    /// <param name="reason">The reason why the URI is malformed.</param>
    public MalformedUriException(string configurationKey, string malformedUri, string reason)
        : base($"Malformed op:// URI in configuration key '{configurationKey}': {reason}. " +
               $"Expected format: op://<vault>/<item>/<field> or op://<vault>/<item>/<section>/<field>. " +
               $"Received: {malformedUri}")
    {
        ConfigurationKey = configurationKey;
        MalformedUri = malformedUri;
    }
}
