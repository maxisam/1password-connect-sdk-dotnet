// Implementation: OnePasswordClient
// Feature: 001-onepassword-sdk

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OnePassword.Sdk.Exceptions;
using OnePassword.Sdk.Internal;
using OnePassword.Sdk.Models;
using OnePassword.Sdk.Resilience;

namespace OnePassword.Sdk.Client;

/// <summary>
/// Implementation of IOnePasswordClient for 1Password Connect API.
/// </summary>
public class OnePasswordClient : IOnePasswordClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OnePasswordClient> _logger;
    private readonly OnePasswordClientOptions _options;
    private readonly SemaphoreSlim _requestSemaphore = new(int.MaxValue);
    private int _activeRequests;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the OnePasswordClient.
    /// </summary>
    /// <param name="options">Client configuration options</param>
    /// <param name="logger">Optional logger instance</param>
    public OnePasswordClient(OnePasswordClientOptions options, ILogger<OnePasswordClient>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _options.Validate(); // Validates HTTPS, token presence, etc.

        _logger = logger ?? NullLogger<OnePasswordClient>.Instance;

        // Create HTTP client with Polly retry and timeout policies
        _httpClient = CreateHttpClient();

        _logger.LogInformation("OnePasswordClient initialized with server {ConnectServer} [CorrelationId: {CorrelationId}]",
            _options.ConnectServer, CorrelationContext.GetCorrelationId());
    }

    #region Vault Operations

    /// <summary>
    /// Lists all vaults accessible with the configured access token.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Collection of accessible vaults</returns>
    /// <exception cref="AuthenticationException">Thrown when authentication fails</exception>
    /// <exception cref="NetworkException">Thrown when network request fails after retries</exception>
    public async Task<IEnumerable<Vault>> ListVaultsAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendRequestAsync<Vault[]>(
            HttpMethod.Get,
            "/v1/vaults",
            cancellationToken);

        _logger.LogInformation("Listed {Count} vaults successfully", response.Length);
        return response;
    }

    /// <summary>
    /// Retrieves a specific vault by its ID.
    /// </summary>
    /// <param name="vaultId">The UUID of the vault to retrieve</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The requested vault</returns>
    /// <exception cref="ArgumentException">Thrown when vaultId is null or whitespace</exception>
    /// <exception cref="VaultNotFoundException">Thrown when the vault does not exist</exception>
    /// <exception cref="AccessDeniedException">Thrown when access to the vault is denied</exception>
    /// <exception cref="AuthenticationException">Thrown when authentication fails</exception>
    /// <exception cref="NetworkException">Thrown when network request fails after retries</exception>
    public async Task<Vault> GetVaultAsync(string vaultId, CancellationToken cancellationToken = default)
    {
        ThrowIfNullOrWhiteSpace(vaultId, nameof(vaultId));

        try
        {
            var vault = await SendRequestAsync<Vault>(
                HttpMethod.Get,
                $"/v1/vaults/{vaultId}",
                cancellationToken);

            return vault;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new VaultNotFoundException(vaultId, ex);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new AccessDeniedException($"Access denied to vault '{vaultId}'", vaultId);
        }
    }

    /// <summary>
    /// Retrieves a vault by its title/name.
    /// </summary>
    /// <param name="title">The name of the vault to retrieve</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The requested vault</returns>
    /// <exception cref="ArgumentException">Thrown when title is null or whitespace</exception>
    /// <exception cref="VaultNotFoundException">Thrown when no vault with the specified title exists</exception>
    /// <exception cref="AuthenticationException">Thrown when authentication fails</exception>
    /// <exception cref="NetworkException">Thrown when network request fails after retries</exception>
    public async Task<Vault> GetVaultByTitleAsync(string title, CancellationToken cancellationToken = default)
    {
        ThrowIfNullOrWhiteSpace(title, nameof(title));

        var vaults = await ListVaultsAsync(cancellationToken);
        var vault = vaults.FirstOrDefault(v => v.Name.Equals(title, StringComparison.Ordinal));

        if (vault == null)
        {
            throw new VaultNotFoundException(title);
        }

        return vault;
    }

    #endregion

    #region Item Operations

    /// <summary>
    /// Lists all items in a specific vault.
    /// </summary>
    /// <param name="vaultId">The UUID of the vault containing the items</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Collection of items in the vault</returns>
    /// <exception cref="ArgumentException">Thrown when vaultId is null or whitespace</exception>
    /// <exception cref="VaultNotFoundException">Thrown when the vault does not exist</exception>
    /// <exception cref="AccessDeniedException">Thrown when access to the vault is denied</exception>
    /// <exception cref="AuthenticationException">Thrown when authentication fails</exception>
    /// <exception cref="NetworkException">Thrown when network request fails after retries</exception>
    public async Task<IEnumerable<Item>> ListItemsAsync(string vaultId, CancellationToken cancellationToken = default)
    {
        ThrowIfNullOrWhiteSpace(vaultId, nameof(vaultId));

        try
        {
            var response = await SendRequestAsync<Item[]>(
                HttpMethod.Get,
                $"/v1/vaults/{vaultId}/items",
                cancellationToken);

            _logger.LogInformation("Listed {Count} items from vault {VaultId}", response.Length, vaultId);
            return response;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new VaultNotFoundException(vaultId, ex);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new AccessDeniedException($"Access denied to vault '{vaultId}'", vaultId);
        }
    }

    /// <summary>
    /// Retrieves a specific item from a vault by its ID, including all fields and secret values.
    /// </summary>
    /// <param name="vaultId">The UUID of the vault containing the item</param>
    /// <param name="itemId">The UUID of the item to retrieve</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The requested item with all fields</returns>
    /// <exception cref="ArgumentException">Thrown when vaultId or itemId is null or whitespace</exception>
    /// <exception cref="ItemNotFoundException">Thrown when the item does not exist</exception>
    /// <exception cref="AccessDeniedException">Thrown when access to the item is denied</exception>
    /// <exception cref="SecretSizeExceededException">Thrown when a field value exceeds 1MB</exception>
    /// <exception cref="AuthenticationException">Thrown when authentication fails</exception>
    /// <exception cref="NetworkException">Thrown when network request fails after retries</exception>
    public async Task<Item> GetItemAsync(string vaultId, string itemId, CancellationToken cancellationToken = default)
    {
        ThrowIfNullOrWhiteSpace(vaultId, nameof(vaultId));
        ThrowIfNullOrWhiteSpace(itemId, nameof(itemId));

        try
        {
            var item = await SendRequestAsync<Item>(
                HttpMethod.Get,
                $"/v1/vaults/{vaultId}/items/{itemId}",
                cancellationToken);

            // Validate secret sizes (FR-023)
            ValidateSecretSizes(item, vaultId, itemId);

            return item;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new ItemNotFoundException(vaultId, itemId, ex);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new AccessDeniedException($"Access denied to item '{itemId}' in vault '{vaultId}'", vaultId, itemId);
        }
    }

    /// <summary>
    /// Retrieves an item from a vault by its title, including all fields and secret values.
    /// </summary>
    /// <param name="vaultId">The UUID of the vault containing the item</param>
    /// <param name="title">The title of the item to retrieve</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The requested item with all fields</returns>
    /// <exception cref="ArgumentException">Thrown when vaultId or title is null or whitespace</exception>
    /// <exception cref="ItemNotFoundException">Thrown when no item with the specified title exists</exception>
    /// <exception cref="SecretSizeExceededException">Thrown when a field value exceeds 1MB</exception>
    /// <exception cref="AuthenticationException">Thrown when authentication fails</exception>
    /// <exception cref="NetworkException">Thrown when network request fails after retries</exception>
    public async Task<Item> GetItemByTitleAsync(string vaultId, string title, CancellationToken cancellationToken = default)
    {
        ThrowIfNullOrWhiteSpace(vaultId, nameof(vaultId));
        ThrowIfNullOrWhiteSpace(title, nameof(title));

        var items = await ListItemsAsync(vaultId, cancellationToken);
        var item = items.FirstOrDefault(i => i.Title.Equals(title, StringComparison.Ordinal));

        if (item == null)
        {
            throw new ItemNotFoundException(vaultId, title);
        }

        // Need to fetch full item with fields
        return await GetItemAsync(vaultId, item.Id, cancellationToken);
    }

    #endregion

    #region Secret Field Operations

    /// <summary>
    /// Retrieves a specific secret value from an item's field.
    /// </summary>
    /// <param name="vaultId">The UUID of the vault containing the item</param>
    /// <param name="itemId">The UUID of the item containing the field</param>
    /// <param name="fieldLabel">The label of the field to retrieve</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The secret value from the specified field</returns>
    /// <exception cref="ArgumentException">Thrown when any parameter is null or whitespace</exception>
    /// <exception cref="FieldNotFoundException">Thrown when the field does not exist in the item</exception>
    /// <exception cref="ItemNotFoundException">Thrown when the item does not exist</exception>
    /// <exception cref="AuthenticationException">Thrown when authentication fails</exception>
    /// <exception cref="NetworkException">Thrown when network request fails after retries</exception>
    public async Task<string> GetSecretAsync(string vaultId, string itemId, string fieldLabel, CancellationToken cancellationToken = default)
    {
        ThrowIfNullOrWhiteSpace(vaultId, nameof(vaultId));
        ThrowIfNullOrWhiteSpace(itemId, nameof(itemId));
        ThrowIfNullOrWhiteSpace(fieldLabel, nameof(fieldLabel));

        var item = await GetItemAsync(vaultId, itemId, cancellationToken);

        var field = item.Fields.FirstOrDefault(f => f.Label?.Equals(fieldLabel, StringComparison.Ordinal) == true);

        if (field == null)
        {
            throw new FieldNotFoundException(vaultId, itemId, fieldLabel);
        }

        _logger.LogInformation("Retrieved secret for field {FieldLabel} from item {ItemId}", fieldLabel, itemId);

        return field.Value ?? string.Empty;
    }

    #endregion

    #region Batch Operations

    /// <summary>
    /// Retrieves multiple secrets in a single batch operation using op:// URI references.
    /// </summary>
    /// <param name="references">Collection of op:// URIs (e.g., "op://vault/item/field")</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Dictionary mapping each URI to its resolved secret value</returns>
    /// <exception cref="ArgumentNullException">Thrown when references is null</exception>
    /// <exception cref="ArgumentException">Thrown when references collection is empty</exception>
    /// <exception cref="BatchSizeExceededException">Thrown when more than 100 references are provided</exception>
    /// <exception cref="MalformedUriException">Thrown when any URI is not a valid op:// format</exception>
    /// <exception cref="VaultNotFoundException">Thrown when a referenced vault does not exist</exception>
    /// <exception cref="ItemNotFoundException">Thrown when a referenced item does not exist</exception>
    /// <exception cref="FieldNotFoundException">Thrown when a referenced field does not exist</exception>
    /// <exception cref="BatchTimeoutException">Thrown when batch operation exceeds 10 seconds</exception>
    /// <exception cref="AuthenticationException">Thrown when authentication fails</exception>
    /// <exception cref="NetworkException">Thrown when network request fails after retries</exception>
    /// <remarks>
    /// This method optimizes API calls by fetching each unique vault+item combination only once.
    /// Duplicate URIs are automatically deduplicated. Maximum batch size is 100 URIs with a 10-second timeout.
    /// </remarks>
    public async Task<IDictionary<string, string>> GetSecretsAsync(
        IEnumerable<string> references,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(references);

        var referenceList = references.ToList();

        if (referenceList.Count == 0)
        {
            throw new ArgumentException("References collection cannot be empty", nameof(references));
        }

        // Validate batch size (FR-022)
        if (referenceList.Count > 100)
        {
            throw new BatchSizeExceededException(referenceList.Count, 100);
        }

        // Parse and validate all URIs (fail-fast)
        var parsedReferences = new List<(string originalUri, string vault, string item, string? section, string field)>();

        foreach (var uri in referenceList)
        {
            if (!TryParseSecretReference(uri, out var parsed))
            {
                throw new MalformedUriException("batch", uri, "Invalid op:// URI format");
            }
            parsedReferences.Add((uri, parsed.vault, parsed.item, parsed.section, parsed.field));
        }

        // Deduplicate URIs (FR-019)
        var uniqueReferences = parsedReferences
            .GroupBy(r => r.originalUri)
            .Select(g => g.First())
            .ToList();

        // Group by vault+item to minimize API calls
        var itemGroups = uniqueReferences
            .GroupBy(r => (r.vault, r.item))
            .ToList();

        var results = new Dictionary<string, string>();
        var startTime = DateTime.UtcNow;

        try
        {
            // First, resolve all vault names to vault UUIDs
            var vaultNameToId = new Dictionary<string, string>();
            var uniqueVaultNames = itemGroups.Select(g => g.Key.vault).Distinct().ToList();

            foreach (var vaultName in uniqueVaultNames)
            {
                // Try to use as UUID first (if it's already a UUID)
                // Otherwise, look up by name
                if (IsUuid(vaultName))
                {
                    vaultNameToId[vaultName] = vaultName;
                }
                else
                {
                    var vault = await GetVaultByTitleAsync(vaultName, cancellationToken);
                    vaultNameToId[vaultName] = vault.Id;
                }
            }

            // Fetch all items in parallel (within timeout)
            var fetchTasks = itemGroups.Select(async group =>
            {
                var vaultId = vaultNameToId[group.Key.vault];

                // Try to get item - could be UUID or title
                Item item;
                if (IsUuid(group.Key.item))
                {
                    item = await GetItemAsync(vaultId, group.Key.item, cancellationToken);
                }
                else
                {
                    item = await GetItemByTitleAsync(vaultId, group.Key.item, cancellationToken);
                }

                return (group, item);
            });

            var fetchedItems = await Task.WhenAll(fetchTasks);

            // Extract fields for each reference
            foreach (var (group, item) in fetchedItems)
            {
                foreach (var reference in group)
                {
                    var field = item.Fields.FirstOrDefault(f =>
                        f.Label?.Equals(reference.field, StringComparison.Ordinal) == true);

                    if (field == null)
                    {
                        throw new FieldNotFoundException(reference.vault, reference.item, reference.field);
                    }

                    results[reference.originalUri] = field.Value ?? string.Empty;
                }
            }

            // Check timeout (FR-024)
            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalSeconds > 10)
            {
                throw new BatchTimeoutException(TimeSpan.FromSeconds(10));
            }

            _logger.LogInformation("Batch retrieved {Count} secrets in {Duration}ms",
                results.Count, elapsed.TotalMilliseconds);

            return results;
        }
        catch (OperationCanceledException ex)
        {
            throw new BatchTimeoutException(TimeSpan.FromSeconds(10), ex);
        }
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Releases all resources used by the OnePasswordClient.
    /// </summary>
    /// <remarks>
    /// FR-011: Implements graceful shutdown by allowing in-flight requests to complete
    /// within a grace period (5 seconds), then disposes the underlying HttpClient.
    /// After calling Dispose, new requests will be rejected with ObjectDisposedException.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Mark as disposed to reject new requests
        _disposed = true;

        // FR-011: Wait for in-flight requests to complete (with 5-second grace period)
        var gracePeriod = TimeSpan.FromSeconds(5);
        var deadline = DateTime.UtcNow.Add(gracePeriod);

        while (_activeRequests > 0 && DateTime.UtcNow < deadline)
        {
            _logger.LogDebug("Waiting for {ActiveRequests} in-flight requests to complete", _activeRequests);
            Thread.Sleep(100); // Poll every 100ms
        }

        if (_activeRequests > 0)
        {
            _logger.LogWarning(
                "Disposing client with {ActiveRequests} in-flight requests still active after {GracePeriod}s grace period",
                _activeRequests, gracePeriod.TotalSeconds);
        }
        else
        {
            _logger.LogInformation("All in-flight requests completed before disposal");
        }

        _httpClient?.Dispose();
        _requestSemaphore?.Dispose();
        _logger.LogInformation("OnePasswordClient disposed");
    }

    #endregion

    #region Private Helper Methods

    private HttpClient CreateHttpClient()
    {
        // FR-001: Create HttpClient with Polly v8 resilience pipeline
        // Build the resilience pipeline (timeout → retry → circuit breaker)
        var pipeline = ResiliencePolicyBuilder.BuildResiliencePipeline(_options);

        // Create the base handler
        var baseHandler = new HttpClientHandler();

        // Wrap with resilience handler
        var resilienceHandler = new ResilienceHttpMessageHandler(pipeline, baseHandler);

        // Create HttpClient with resilience-enabled handler
        var client = new HttpClient(resilienceHandler, disposeHandler: true)
        {
            BaseAddress = new Uri(_options.ConnectServer),
            // Note: Timeout is now managed by the resilience pipeline's TimeoutStrategy
            // Set HttpClient.Timeout to Infinite to prevent conflicts
            Timeout = Timeout.InfiniteTimeSpan
        };

        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.Token}");
        client.DefaultRequestHeaders.Add("Accept", "application/json");

        _logger.LogDebug("HttpClient created with resilience pipeline: {MaxRetries} retries, {CircuitBreakerThreshold} circuit threshold",
            _options.MaxRetries, _options.CircuitBreakerFailureThreshold);

        return client;
    }

    private async Task<T> SendRequestAsync<T>(
        HttpMethod method,
        string path,
        CancellationToken cancellationToken)
    {
        // FR-011: Reject new requests after disposal
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(OnePasswordClient), "Cannot send requests after client has been disposed");
        }

        // Track in-flight request
        Interlocked.Increment(ref _activeRequests);
        try
        {
            // FR-020: Manual retry logic removed - now handled by resilience pipeline
            var request = new HttpRequestMessage(method, path);

            // FR-009: Log request at Debug level
            _logger.LogDebug("Sending {Method} request to {Path} [CorrelationId: {CorrelationId}]",
                method, path, CorrelationContext.GetCorrelationId());

            var response = await _httpClient.SendAsync(request, cancellationToken);

            // FR-010: Permanent errors (401, 403, 404) are not retried by the pipeline
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new AuthenticationException("Authentication failed: invalid or expired token");
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);

            if (result == null)
            {
                throw new OnePasswordException($"Failed to deserialize response from {path}");
            }

            // FR-009: Log successful request at Information level
            _logger.LogInformation("Request to {Path} completed successfully [CorrelationId: {CorrelationId}]",
                path, CorrelationContext.GetCorrelationId());

            return result;
        }
        catch (HttpRequestException ex) when (ex.StatusCode != HttpStatusCode.Unauthorized)
        {
            // Network or HTTP error after all retries exhausted
            _logger.LogError(ex, "Request to {Path} failed [CorrelationId: {CorrelationId}]",
                path, CorrelationContext.GetCorrelationId());
            throw new NetworkException($"Request to {path} failed", ex);
        }
        catch (TimeoutException ex)
        {
            // FR-017: Timeout exceeded (either per-request or cumulative)
            _logger.LogError(ex, "Request to {Path} timed out [CorrelationId: {CorrelationId}]",
                path, CorrelationContext.GetCorrelationId());
            throw new NetworkException($"Request to {path} timed out", ex);
        }
        finally
        {
            // Release in-flight request tracking
            Interlocked.Decrement(ref _activeRequests);
        }
    }

    private static void ThrowIfNullOrWhiteSpace(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Value cannot be null or whitespace.", paramName);
        }
    }

    private static bool IsUuid(string value)
    {
        // UUID format: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx (26 chars with hyphens)
        // 1Password also uses compact UUIDs without hyphens (26 chars)
        return Guid.TryParse(value, out _);
    }

    private static void ValidateSecretSizes(Item item, string vaultId, string itemId)
    {
        const long maxSizeBytes = 1048576; // 1MB

        foreach (var field in item.Fields)
        {
            if (field.Value != null)
            {
                var sizeBytes = System.Text.Encoding.UTF8.GetByteCount(field.Value);
                if (sizeBytes > maxSizeBytes)
                {
                    throw new SecretSizeExceededException(vaultId, itemId, field.Label ?? field.Id, sizeBytes, maxSizeBytes);
                }
            }
        }
    }

    private static bool TryParseSecretReference(
        string uri,
        out (string vault, string item, string? section, string field) parsed)
    {
        parsed = default;

        if (!uri.StartsWith("op://", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var path = uri.Substring(5); // Remove "op://"
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 3 || parts.Length > 4)
        {
            return false;
        }

        // Decode URL-encoded components
        var decodedParts = parts.Select(Uri.UnescapeDataString).ToArray();

        if (decodedParts.Any(string.IsNullOrWhiteSpace))
        {
            return false;
        }

        if (parts.Length == 3)
        {
            // op://vault/item/field
            parsed = (decodedParts[0], decodedParts[1], null, decodedParts[2]);
        }
        else
        {
            // op://vault/item/section/field
            parsed = (decodedParts[0], decodedParts[1], decodedParts[2], decodedParts[3]);
        }

        return true;
    }

    #endregion
}
