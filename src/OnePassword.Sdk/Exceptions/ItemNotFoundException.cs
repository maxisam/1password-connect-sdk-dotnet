// API Contract: Item Not Found Exception
// Feature: 001-onepassword-sdk

namespace OnePassword.Sdk.Exceptions;

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
    /// <summary>
    /// Gets the vault ID where the item was not found.
    /// </summary>
    public string VaultId { get; }

    /// <summary>
    /// Gets the item ID that was not found.
    /// </summary>
    public string ItemId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemNotFoundException"/> class.
    /// </summary>
    /// <param name="vaultId">The vault ID where the item was not found.</param>
    /// <param name="itemId">The item ID that was not found.</param>
    public ItemNotFoundException(string vaultId, string itemId)
        : base($"Item '{itemId}' not found in vault '{vaultId}'")
    {
        VaultId = vaultId;
        ItemId = itemId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemNotFoundException"/> class with an inner exception.
    /// </summary>
    /// <param name="vaultId">The vault ID where the item was not found.</param>
    /// <param name="itemId">The item ID that was not found.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ItemNotFoundException(string vaultId, string itemId, Exception innerException)
        : base($"Item '{itemId}' not found in vault '{vaultId}'", innerException)
    {
        VaultId = vaultId;
        ItemId = itemId;
    }
}
