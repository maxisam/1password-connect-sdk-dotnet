// API Contract: Access Denied Exception
// Feature: 001-onepassword-sdk

namespace OnePassword.Sdk.Exceptions;

/// <summary>
/// Thrown when access to a vault or item is denied due to insufficient permissions.
/// </summary>
/// <remarks>
/// Corresponds to FR-025: access denied exception type
/// Corresponds to FR-031, FR-034: authorization failure handling
///
/// Typical causes:
/// - Access token lacks permission to access specific vault
/// - Vault permissions changed after application startup
/// - Vault exists but is not accessible to this token
///
/// Resolution: Verify service account has appropriate vault permissions in 1Password.
/// </remarks>
public class AccessDeniedException : OnePasswordException
{
    /// <summary>
    /// Gets the vault ID that was being accessed when the exception occurred.
    /// </summary>
    public string? VaultId { get; }

    /// <summary>
    /// Gets the item ID that was being accessed when the exception occurred.
    /// </summary>
    public string? ItemId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessDeniedException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="vaultId">The vault ID that access was denied to.</param>
    /// <param name="itemId">The item ID that access was denied to.</param>
    public AccessDeniedException(string message, string? vaultId = null, string? itemId = null)
        : base(message)
    {
        VaultId = vaultId;
        ItemId = itemId;
    }
}
