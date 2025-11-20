// API Contract: Secret Size Exceeded Exception
// Feature: 001-onepassword-sdk

namespace OnePassword.Sdk.Exceptions;

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
    /// <summary>
    /// Gets the vault ID where the oversized secret was found.
    /// </summary>
    public string VaultId { get; }

    /// <summary>
    /// Gets the item ID where the oversized secret was found.
    /// </summary>
    public string ItemId { get; }

    /// <summary>
    /// Gets the field label of the oversized secret.
    /// </summary>
    public string FieldLabel { get; }

    /// <summary>
    /// Gets the actual size of the secret in bytes.
    /// </summary>
    public long ActualSizeBytes { get; }

    /// <summary>
    /// Gets the maximum allowed size in bytes.
    /// </summary>
    public long MaximumSizeBytes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretSizeExceededException"/> class.
    /// </summary>
    /// <param name="vaultId">The vault ID where the oversized secret was found.</param>
    /// <param name="itemId">The item ID where the oversized secret was found.</param>
    /// <param name="fieldLabel">The field label of the oversized secret.</param>
    /// <param name="actualSizeBytes">The actual size of the secret in bytes.</param>
    /// <param name="maximumSizeBytes">The maximum allowed size in bytes (default 1MB).</param>
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
