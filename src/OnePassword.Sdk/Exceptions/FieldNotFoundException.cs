// API Contract: Field Not Found Exception
// Feature: 001-onepassword-sdk

namespace OnePassword.Sdk.Exceptions;

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
    /// <summary>
    /// Gets the vault ID where the field was not found.
    /// </summary>
    public string VaultId { get; }

    /// <summary>
    /// Gets the item ID where the field was not found.
    /// </summary>
    public string ItemId { get; }

    /// <summary>
    /// Gets the field label that was not found.
    /// </summary>
    public string FieldLabel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldNotFoundException"/> class.
    /// </summary>
    /// <param name="vaultId">The vault ID where the field was not found.</param>
    /// <param name="itemId">The item ID where the field was not found.</param>
    /// <param name="fieldLabel">The field label that was not found.</param>
    public FieldNotFoundException(string vaultId, string itemId, string fieldLabel)
        : base($"Field '{fieldLabel}' not found in item '{itemId}' in vault '{vaultId}'")
    {
        VaultId = vaultId;
        ItemId = itemId;
        FieldLabel = fieldLabel;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldNotFoundException"/> class with an inner exception.
    /// </summary>
    /// <param name="vaultId">The vault ID where the field was not found.</param>
    /// <param name="itemId">The item ID where the field was not found.</param>
    /// <param name="fieldLabel">The field label that was not found.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public FieldNotFoundException(string vaultId, string itemId, string fieldLabel, Exception innerException)
        : base($"Field '{fieldLabel}' not found in item '{itemId}' in vault '{vaultId}'", innerException)
    {
        VaultId = vaultId;
        ItemId = itemId;
        FieldLabel = fieldLabel;
    }
}
