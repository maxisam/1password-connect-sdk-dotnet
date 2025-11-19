// API Contract: Exception Hierarchy
// Feature: 001-onepassword-sdk
// Purpose: Specific exception types for different error scenarios

using System;

namespace OnePassword.Sdk.Exceptions
{
    /// <summary>
    /// Base exception for all 1Password SDK errors.
    /// </summary>
    /// <remarks>
    /// Corresponds to FR-025: SDK MUST throw specific exceptions for different error types
    ///
    /// All SDK exceptions inherit from this base class, allowing consumers to catch
    /// all 1Password errors with a single catch block if desired.
    ///
    /// Exception hierarchy follows Constitution Principle III (API Simplicity) by providing
    /// clear, specific exception types with actionable error messages (FR-026, FR-029, FR-032).
    /// </remarks>
    public class OnePasswordException : Exception
    {
        public OnePasswordException(string message) : base(message) { }
        public OnePasswordException(string message, Exception innerException) : base(message, innerException) { }
    }

    #region Authentication & Authorization Exceptions (FR-008, FR-009, FR-025)

    /// <summary>
    /// Thrown when authentication fails due to invalid or expired token.
    /// </summary>
    /// <remarks>
    /// Corresponds to FR-025: authentication failure exception type
    /// Corresponds to FR-021: token expiration error
    /// Corresponds to FR-008, FR-009: authentication error handling
    ///
    /// Typical causes:
    /// - Invalid service account token
    /// - Expired token (for long-running applications)
    /// - Token revoked by administrator
    ///
    /// Resolution: Verify token is correct, check token expiration, restart application
    /// with valid token.
    ///
    /// Security: Error message MUST NOT include the token value (FR-036).
    /// </remarks>
    public class AuthenticationException : OnePasswordException
    {
        public AuthenticationException(string message) : base(message) { }
        public AuthenticationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Thrown when access to a vault or item is denied due to insufficient permissions.
    /// </summary>
    /// <remarks>
    /// Corresponds to FR-025: access denied exception type
    /// Corresponds to FR-031, FR-034: authorization failure handling
    ///
    /// Typical causes:
    /// - Service account token lacks permission to access specific vault
    /// - Vault permissions changed after application startup
    /// - Vault exists but is not accessible to this token
    ///
    /// Resolution: Verify service account has appropriate vault permissions in 1Password.
    /// </remarks>
    public class AccessDeniedException : OnePasswordException
    {
        public string VaultId { get; }
        public string ItemId { get; }

        public AccessDeniedException(string message, string vaultId = null, string itemId = null)
            : base(message)
        {
            VaultId = vaultId;
            ItemId = itemId;
        }
    }

    #endregion

    #region Resource Not Found Exceptions (FR-023, FR-024, FR-025, FR-026)

    /// <summary>
    /// Thrown when a requested vault does not exist or is not accessible.
    /// </summary>
    /// <remarks>
    /// Corresponds to FR-025: vault not found exception type
    /// Corresponds to FR-026: context included in error messages
    ///
    /// Error message format (FR-026): "Vault '{vaultId}' not found or not accessible"
    /// </remarks>
    public class VaultNotFoundException : OnePasswordException
    {
        public string VaultId { get; }

        public VaultNotFoundException(string vaultId)
            : base($"Vault '{vaultId}' not found or not accessible")
        {
            VaultId = vaultId;
        }

        public VaultNotFoundException(string vaultId, Exception innerException)
            : base($"Vault '{vaultId}' not found or not accessible", innerException)
        {
            VaultId = vaultId;
        }
    }

    /// <summary>
    /// Thrown when a requested item does not exist in the specified vault.
    /// </summary>
    /// <remarks>
    /// Corresponds to FR-025: item not found exception type
    /// Corresponds to FR-026: context included in error messages
    ///
    /// Error message format (FR-026): "Item '{itemId}' not found in vault '{vaultId}'"
    /// </remarks>
    public class ItemNotFoundException : OnePasswordException
    {
        public string VaultId { get; }
        public string ItemId { get; }

        public ItemNotFoundException(string vaultId, string itemId)
            : base($"Item '{itemId}' not found in vault '{vaultId}'")
        {
            VaultId = vaultId;
            ItemId = itemId;
        }

        public ItemNotFoundException(string vaultId, string itemId, Exception innerException)
            : base($"Item '{itemId}' not found in vault '{vaultId}'", innerException)
        {
            VaultId = vaultId;
            ItemId = itemId;
        }
    }

    /// <summary>
    /// Thrown when a requested field does not exist in the specified item.
    /// </summary>
    /// <remarks>
    /// Corresponds to FR-025: field not found exception type
    /// Corresponds to FR-026: context included in error messages
    ///
    /// Error message format (FR-026):
    /// "Field '{fieldLabel}' not found in item '{itemId}' in vault '{vaultId}'"
    /// </remarks>
    public class FieldNotFoundException : OnePasswordException
    {
        public string VaultId { get; }
        public string ItemId { get; }
        public string FieldLabel { get; }

        public FieldNotFoundException(string vaultId, string itemId, string fieldLabel)
            : base($"Field '{fieldLabel}' not found in item '{itemId}' in vault '{vaultId}'")
        {
            VaultId = vaultId;
            ItemId = itemId;
            FieldLabel = fieldLabel;
        }

        public FieldNotFoundException(string vaultId, string itemId, string fieldLabel, Exception innerException)
            : base($"Field '{fieldLabel}' not found in item '{itemId}' in vault '{vaultId}'", innerException)
        {
            VaultId = vaultId;
            ItemId = itemId;
            FieldLabel = fieldLabel;
        }
    }

    #endregion

    #region Network & Communication Exceptions (FR-028, FR-033)

    /// <summary>
    /// Thrown when network communication with 1Password Connect API fails after retries.
    /// </summary>
    /// <remarks>
    /// Corresponds to FR-025: network error exception type
    /// Corresponds to FR-033: retry transient network failures up to 3 times
    ///
    /// This exception is thrown only AFTER retry attempts have been exhausted.
    /// Transient failures are retried 3 times with exponential backoff (1s, 2s, 4s).
    ///
    /// Typical causes:
    /// - 1Password Connect server unreachable
    /// - DNS resolution failure
    /// - Network connectivity issues
    /// - Firewall blocking connection
    ///
    /// Resolution: Verify Connect server URL, check network connectivity, verify firewall rules.
    /// </remarks>
    public class NetworkException : OnePasswordException
    {
        public int RetryAttempts { get; }

        public NetworkException(string message, int retryAttempts = 3)
            : base($"{message} (after {retryAttempts} retry attempts)")
        {
            RetryAttempts = retryAttempts;
        }

        public NetworkException(string message, Exception innerException, int retryAttempts = 3)
            : base($"{message} (after {retryAttempts} retry attempts)", innerException)
        {
            RetryAttempts = retryAttempts;
        }
    }

    #endregion

    #region URI & Validation Exceptions (FR-026, FR-028, FR-029, FR-031, FR-032)

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
        public string ConfigurationKey { get; }
        public string MalformedUri { get; }

        public MalformedUriException(string configurationKey, string malformedUri, string reason)
            : base($"Malformed op:// URI in configuration key '{configurationKey}': {reason}. " +
                   $"Expected format: op://<vault>/<item>/<field> or op://<vault>/<item>/<section>/<field>. " +
                   $"Received: {malformedUri}")
        {
            ConfigurationKey = configurationKey;
            MalformedUri = malformedUri;
        }
    }

    #endregion

    #region Limit & Constraint Exceptions (FR-022, FR-023, FR-024, FR-028)

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
        public int RequestedCount { get; }
        public int MaximumAllowed { get; }

        public BatchSizeExceededException(int requestedCount, int maximumAllowed = 100)
            : base($"Batch secret retrieval limit exceeded: {requestedCount} secrets requested, maximum is {maximumAllowed}")
        {
            RequestedCount = requestedCount;
            MaximumAllowed = maximumAllowed;
        }
    }

    /// <summary>
    /// Thrown when a secret value exceeds the maximum allowed size.
    /// </summary>
    /// <remarks>
    /// Corresponds to FR-025: secret size exceeded exception type
    /// Corresponds to FR-023, FR-028: enforce maximum of 1MB per secret value
    ///
    /// Error message format (FR-026): "Secret value exceeds maximum size limit: field '{field}'
    /// in item '{item}' in vault '{vault}' is {actualSize}MB (maximum is 1MB)"
    /// </remarks>
    public class SecretSizeExceededException : OnePasswordException
    {
        public string VaultId { get; }
        public string ItemId { get; }
        public string FieldLabel { get; }
        public long ActualSizeBytes { get; }
        public long MaximumSizeBytes { get; }

        public SecretSizeExceededException(string vaultId, string itemId, string fieldLabel, long actualSizeBytes, long maximumSizeBytes = 1048576)
            : base($"Secret value exceeds maximum size limit: field '{fieldLabel}' in item '{itemId}' in vault '{vaultId}' " +
                   $"is {actualSizeBytes / 1048576.0:F2}MB (maximum is {maximumSizeBytes / 1048576}MB)")
        {
            VaultId = vaultId;
            ItemId = itemId;
            FieldLabel = fieldLabel;
            ActualSizeBytes = actualSizeBytes;
            MaximumSizeBytes = maximumSizeBytes;
        }
    }

    /// <summary>
    /// Thrown when batch retrieval operation exceeds the timeout limit.
    /// </summary>
    /// <remarks>
    /// Corresponds to FR-025: timeout exception type
    /// Corresponds to FR-024, FR-028: enforce 10-second total timeout for batch operations
    ///
    /// Error message format: "Batch secret retrieval operation timed out after 10 seconds"
    /// </remarks>
    public class BatchTimeoutException : OnePasswordException
    {
        public TimeSpan Timeout { get; }

        public BatchTimeoutException(TimeSpan timeout)
            : base($"Batch secret retrieval operation timed out after {timeout.TotalSeconds} seconds")
        {
            Timeout = timeout;
        }

        public BatchTimeoutException(TimeSpan timeout, Exception innerException)
            : base($"Batch secret retrieval operation timed out after {timeout.TotalSeconds} seconds", innerException)
        {
            Timeout = timeout;
        }
    }

    #endregion
}
