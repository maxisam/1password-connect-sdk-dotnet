// API Contract: Vault Not Found Exception
// Feature: 001-onepassword-sdk

namespace OnePassword.Sdk.Exceptions;

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
    /// <summary>
    /// Gets the vault ID that was not found.
    /// </summary>
    public string VaultId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultNotFoundException"/> class.
    /// </summary>
    /// <param name="vaultId">The vault ID that was not found.</param>
    public VaultNotFoundException(string vaultId)
        : base($"Vault '{vaultId}' not found or not accessible")
    {
        VaultId = vaultId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultNotFoundException"/> class with an inner exception.
    /// </summary>
    /// <param name="vaultId">The vault ID that was not found.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public VaultNotFoundException(string vaultId, Exception innerException)
        : base($"Vault '{vaultId}' not found or not accessible", innerException)
    {
        VaultId = vaultId;
    }
}
