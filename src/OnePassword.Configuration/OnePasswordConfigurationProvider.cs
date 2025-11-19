// Implementation: OnePasswordConfigurationProvider
// Feature: 001-onepassword-sdk

using Microsoft.Extensions.Configuration;
using OnePassword.Sdk.Client;
using OnePassword.Sdk.Exceptions;
using System.Collections.Concurrent;

namespace OnePassword.Configuration;

/// <summary>
/// Configuration provider that resolves 1Password op:// URIs to actual secret values.
/// </summary>
internal class OnePasswordConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly OnePasswordConfigurationSource _source;
    private readonly ConcurrentDictionary<string, string> _secretCache = new();
    private IOnePasswordClient? _client;
    private bool _disposed;

    public OnePasswordConfigurationProvider(OnePasswordConfigurationSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public override void Load()
    {
        // Validate that we have credentials
        if (string.IsNullOrWhiteSpace(_source.ConnectServer))
        {
            throw new InvalidOperationException(
                "OnePassword:ConnectServer not configured. " +
                "Add it to appsettings.json or set OnePassword__ConnectServer environment variable.");
        }

        if (string.IsNullOrWhiteSpace(_source.Token))
        {
            throw new InvalidOperationException(
                "OnePassword:Token not configured. " +
                "Add it to appsettings.json or set OnePassword__Token environment variable.");
        }

        // Create 1Password client
        var options = new OnePasswordClientOptions
        {
            ConnectServer = _source.ConnectServer,
            Token = _source.Token
        };

        _client = new OnePasswordClient(options);

        try
        {
            // Scan all configuration values for op:// URIs
            var opUris = ScanForOpUris();

            if (opUris.Count == 0)
            {
                // No op:// URIs found, nothing to resolve
                return;
            }

            // Batch retrieve all secrets
            var resolvedSecrets = _client.GetSecretsAsync(opUris.Values).GetAwaiter().GetResult();

            // Cache resolved secrets
            foreach (var kvp in opUris)
            {
                var configKey = kvp.Key;
                var opUri = kvp.Value;

                if (resolvedSecrets.TryGetValue(opUri, out var secretValue))
                {
                    _secretCache[configKey] = secretValue;
                    // Update Data dictionary with resolved value
                    Data[configKey] = secretValue;
                }
            }
        }
        catch (Exception ex) when (ex is OnePasswordException)
        {
            // Re-throw 1Password-specific exceptions as-is (already have context)
            throw;
        }
        catch (Exception ex)
        {
            throw new OnePasswordException(
                "Failed to load secrets from 1Password Connect API. " +
                "Ensure ConnectServer is accessible and Token is valid.", ex);
        }
    }

    public override bool TryGet(string key, out string? value)
    {
        // Check if this key has a cached secret
        if (_secretCache.TryGetValue(key, out var cachedSecret))
        {
            value = cachedSecret;
            return true;
        }

        // Fall back to base implementation
        return base.TryGet(key, out value);
    }

    private Dictionary<string, string> ScanForOpUris()
    {
        var opUris = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Scan all keys in Data for op:// URIs
        foreach (var kvp in Data)
        {
            if (kvp.Value != null && kvp.Value.StartsWith("op://", StringComparison.OrdinalIgnoreCase))
            {
                opUris[kvp.Key] = kvp.Value;
            }
        }

        return opUris;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _client?.Dispose();
        _disposed = true;
    }
}
