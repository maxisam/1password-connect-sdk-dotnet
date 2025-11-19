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
using Polly;
using Polly.Extensions.Http;

namespace OnePassword.Sdk.Client;

/// <summary>
/// Implementation of IOnePasswordClient for 1Password Connect API.
/// </summary>
public class OnePasswordClient : IOnePasswordClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OnePasswordClient> _logger;
    private readonly OnePasswordClientOptions _options;
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

    public async Task<IEnumerable<Vault>> ListVaultsAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendRequestAsync<Vault[]>(
            HttpMethod.Get,
            "/v1/vaults",
            cancellationToken);

        _logger.LogInformation("Listed {Count} vaults successfully", response.Length);
        return response;
    }

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
            // Fetch all items in parallel (within timeout)
            var fetchTasks = itemGroups.Select(async group =>
            {
                var item = await GetItemByTitleAsync(group.Key.vault, group.Key.item, cancellationToken);
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

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _httpClient?.Dispose();
        _logger.LogInformation("OnePasswordClient disposed");
        _disposed = true;
    }

    #endregion

    #region Private Helper Methods

    private HttpClient CreateHttpClient()
    {
        // Note: With Polly v8, retry policy is handled differently
        // For simplicity, we'll use a basic HttpClient and handle retries in SendRequestAsync
        var client = new HttpClient
        {
            BaseAddress = new Uri(_options.ConnectServer),
            Timeout = _options.Timeout
        };

        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.Token}");
        client.DefaultRequestHeaders.Add("Accept", "application/json");

        return client;
    }

    private async Task<T> SendRequestAsync<T>(
        HttpMethod method,
        string path,
        CancellationToken cancellationToken)
    {
        var retryCount = 0;
        Exception? lastException = null;

        while (retryCount <= _options.MaxRetries)
        {
            try
            {
                var request = new HttpRequestMessage(method, path);
                var response = await _httpClient.SendAsync(request, cancellationToken);

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

                return result;
            }
            catch (HttpRequestException ex) when (IsTransientError(ex) && retryCount < _options.MaxRetries)
            {
                lastException = ex;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                retryCount++;

                _logger.LogWarning("Retry attempt {RetryCount} of {MaxRetries} after {Delay}ms",
                    retryCount, _options.MaxRetries, delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && retryCount < _options.MaxRetries)
            {
                lastException = ex;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                retryCount++;

                _logger.LogWarning("Retry attempt {RetryCount} of {MaxRetries} after timeout",
                    retryCount, _options.MaxRetries);

                await Task.Delay(delay, cancellationToken);
            }
            catch (AuthenticationException)
            {
                throw;
            }
            catch (HttpRequestException)
            {
                throw;
            }
        }

        // All retries exhausted
        _logger.LogError(lastException, "Request failed after {RetryCount} retries [CorrelationId: {CorrelationId}]",
            retryCount, CorrelationContext.GetCorrelationId());
        throw new NetworkException("Request failed after retries", lastException!, retryCount);
    }

    private static bool IsTransientError(HttpRequestException ex)
    {
        return ex.InnerException is System.Net.Sockets.SocketException ||
               ex.StatusCode is HttpStatusCode.RequestTimeout or
                               HttpStatusCode.ServiceUnavailable or
                               HttpStatusCode.GatewayTimeout;
    }

    private static void ThrowIfNullOrWhiteSpace(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Value cannot be null or whitespace.", paramName);
        }
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
