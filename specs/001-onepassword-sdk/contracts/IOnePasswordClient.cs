// API Contract: IOnePasswordClient
// Feature: 001-onepassword-sdk
// Purpose: Core SDK client interface for programmatic 1Password vault access

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OnePassword.Sdk
{
    /// <summary>
    /// Client interface for interacting with 1Password Connect API.
    /// Provides programmatic access to vaults, items, and secrets.
    /// </summary>
    /// <remarks>
    /// This interface follows the patterns established by official 1Password SDKs
    /// for JavaScript and Python. All operations are asynchronous and support
    /// cancellation via CancellationToken.
    ///
    /// Security: This client handles sensitive data (secrets, tokens). Implementations
    /// MUST NOT log secret values or authentication tokens (per FR-031, FR-032, FR-035, FR-036).
    /// </remarks>
    public interface IOnePasswordClient : IDisposable
    {
        #region Vault Operations (FR-002)

        /// <summary>
        /// Lists all vaults accessible to the authenticated account.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of accessible vaults</returns>
        /// <exception cref="AuthenticationException">Invalid or expired authentication token (FR-025, FR-033)</exception>
        /// <exception cref="NetworkException">Network failure after retries (FR-033)</exception>
        /// <exception cref="OnePasswordException">Other 1Password API errors</exception>
        /// <remarks>
        /// Corresponds to FR-002: SDK MUST provide an API to list available vaults
        /// </remarks>
        Task<IEnumerable<Vault>> ListVaultsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a specific vault by its unique identifier.
        /// </summary>
        /// <param name="vaultId">Vault unique identifier (UUID)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Vault details</returns>
        /// <exception cref="ArgumentException">vaultId is null or empty</exception>
        /// <exception cref="VaultNotFoundException">Vault not found (FR-025)</exception>
        /// <exception cref="AccessDeniedException">Insufficient permissions to access vault (FR-025, FR-034)</exception>
        /// <exception cref="AuthenticationException">Invalid or expired authentication token</exception>
        /// <exception cref="NetworkException">Network failure after retries</exception>
        /// <remarks>
        /// Corresponds to FR-002: SDK MUST provide an API to retrieve vaults
        /// </remarks>
        Task<Vault> GetVaultAsync(string vaultId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a vault by its name/title.
        /// </summary>
        /// <param name="title">Vault name (case-sensitive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Vault details</returns>
        /// <exception cref="ArgumentException">title is null or empty</exception>
        /// <exception cref="VaultNotFoundException">No vault with specified title (FR-025)</exception>
        /// <exception cref="AccessDeniedException">Insufficient permissions</exception>
        /// <exception cref="AuthenticationException">Invalid or expired authentication token</exception>
        /// <exception cref="NetworkException">Network failure after retries</exception>
        /// <remarks>
        /// Convenience method for retrieving vaults by name.
        /// Throws VaultNotFoundException if title doesn't match any accessible vault.
        /// </remarks>
        Task<Vault> GetVaultByTitleAsync(string title, CancellationToken cancellationToken = default);

        #endregion

        #region Item Operations (FR-003)

        /// <summary>
        /// Lists all items in a specified vault.
        /// </summary>
        /// <param name="vaultId">Vault unique identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of items in the vault</returns>
        /// <exception cref="ArgumentException">vaultId is null or empty</exception>
        /// <exception cref="VaultNotFoundException">Vault not found</exception>
        /// <exception cref="AccessDeniedException">Insufficient permissions</exception>
        /// <exception cref="AuthenticationException">Invalid or expired authentication token</exception>
        /// <exception cref="NetworkException">Network failure after retries</exception>
        /// <remarks>
        /// Corresponds to FR-003: SDK MUST provide an API to retrieve items from a vault
        /// </remarks>
        Task<IEnumerable<Item>> ListItemsAsync(string vaultId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a specific item from a vault by item ID.
        /// </summary>
        /// <param name="vaultId">Vault unique identifier</param>
        /// <param name="itemId">Item unique identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Item details including all fields</returns>
        /// <exception cref="ArgumentException">vaultId or itemId is null/empty</exception>
        /// <exception cref="VaultNotFoundException">Vault not found</exception>
        /// <exception cref="ItemNotFoundException">Item not found in vault (FR-025)</exception>
        /// <exception cref="AccessDeniedException">Insufficient permissions</exception>
        /// <exception cref="AuthenticationException">Invalid or expired authentication token</exception>
        /// <exception cref="NetworkException">Network failure after retries</exception>
        /// <exception cref="SecretSizeExceededException">Secret value exceeds 1MB limit (FR-023, FR-028)</exception>
        /// <remarks>
        /// Corresponds to FR-003: SDK MUST provide an API to retrieve a specific item
        /// Security: Returned Item contains secret field values. Do NOT log item.Fields values.
        /// </remarks>
        Task<Item> GetItemAsync(string vaultId, string itemId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves an item from a vault by item title/name.
        /// </summary>
        /// <param name="vaultId">Vault unique identifier</param>
        /// <param name="title">Item title (case-sensitive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Item details including all fields</returns>
        /// <exception cref="ArgumentException">vaultId or title is null/empty</exception>
        /// <exception cref="VaultNotFoundException">Vault not found</exception>
        /// <exception cref="ItemNotFoundException">No item with specified title (FR-025)</exception>
        /// <exception cref="AccessDeniedException">Insufficient permissions</exception>
        /// <exception cref="AuthenticationException">Invalid or expired authentication token</exception>
        /// <exception cref="NetworkException">Network failure after retries</exception>
        /// <exception cref="SecretSizeExceededException">Secret value exceeds 1MB limit</exception>
        /// <remarks>
        /// Convenience method for retrieving items by title.
        /// Security: Returned Item contains secret field values. Do NOT log item.Fields values.
        /// </remarks>
        Task<Item> GetItemByTitleAsync(string vaultId, string title, CancellationToken cancellationToken = default);

        #endregion

        #region Secret Field Operations (FR-004)

        /// <summary>
        /// Retrieves a specific secret field value from an item.
        /// </summary>
        /// <param name="vaultId">Vault unique identifier</param>
        /// <param name="itemId">Item unique identifier</param>
        /// <param name="fieldLabel">Field label/name (case-sensitive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Secret field value as string</returns>
        /// <exception cref="ArgumentException">Any parameter is null/empty</exception>
        /// <exception cref="VaultNotFoundException">Vault not found</exception>
        /// <exception cref="ItemNotFoundException">Item not found</exception>
        /// <exception cref="FieldNotFoundException">Field not found in item (FR-025)</exception>
        /// <exception cref="AccessDeniedException">Insufficient permissions</exception>
        /// <exception cref="AuthenticationException">Invalid or expired authentication token</exception>
        /// <exception cref="NetworkException">Network failure after retries</exception>
        /// <exception cref="SecretSizeExceededException">Secret value exceeds 1MB limit</exception>
        /// <remarks>
        /// Corresponds to FR-004: SDK MUST provide an API to extract a specific secret field value
        ///
        /// Convenience method for direct secret retrieval without parsing full Item.
        /// Returns the field value directly as a string.
        ///
        /// Security: Return value is a SECRET. Do NOT log the returned value.
        /// </remarks>
        Task<string> GetSecretAsync(string vaultId, string itemId, string fieldLabel, CancellationToken cancellationToken = default);

        #endregion

        #region Batch Operations (FR-018, FR-019)

        /// <summary>
        /// Retrieves multiple secrets in a single batch operation.
        /// </summary>
        /// <param name="references">Collection of secret references (op:// URIs or structured references)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary mapping each reference to its resolved secret value</returns>
        /// <exception cref="ArgumentException">references is null or empty</exception>
        /// <exception cref="BatchSizeExceededException">More than 100 secrets requested (FR-022, FR-028)</exception>
        /// <exception cref="MalformedUriException">One or more references are malformed (FR-026, FR-028, FR-029, FR-032)</exception>
        /// <exception cref="VaultNotFoundException">One or more vaults not found</exception>
        /// <exception cref="ItemNotFoundException">One or more items not found</exception>
        /// <exception cref="FieldNotFoundException">One or more fields not found</exception>
        /// <exception cref="AccessDeniedException">Insufficient permissions</exception>
        /// <exception cref="AuthenticationException">Invalid or expired authentication token</exception>
        /// <exception cref="NetworkException">Network failure after retries</exception>
        /// <exception cref="TimeoutException">Batch operation exceeded 10s timeout (FR-024, FR-028)</exception>
        /// <exception cref="SecretSizeExceededException">One or more secrets exceed 1MB limit</exception>
        /// <remarks>
        /// Corresponds to FR-018: Configuration provider MUST collect all op:// URIs and retrieve
        /// secrets using a single batch API call
        ///
        /// This method optimizes retrieval by:
        /// 1. Deduplicating references (FR-019)
        /// 2. Grouping by vault+item to minimize HTTP requests
        /// 3. Retrieving all unique items in parallel where possible
        /// 4. Extracting requested fields from retrieved items
        ///
        /// Constraints:
        /// - Maximum 100 secrets per batch (FR-022)
        /// - 10-second total timeout (FR-024)
        /// - Fail-fast: any error fails the entire batch (FR-027, FR-030)
        ///
        /// Security: Return values are SECRETS. Do NOT log the dictionary values.
        /// </remarks>
        Task<IDictionary<string, string>> GetSecretsAsync(
            IEnumerable<string> references,
            CancellationToken cancellationToken = default);

        #endregion
    }
}
