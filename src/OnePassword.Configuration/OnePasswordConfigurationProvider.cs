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
/// <remarks>
/// This provider scans all previous configuration sources for op:// URIs and resolves them.
/// It respects configuration precedence: if a key has been overridden by a higher-precedence
/// source (e.g., environment variable), the provider skips resolving that key (FR-024).
/// </remarks>
internal class OnePasswordConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly OnePasswordConfigurationSource _source;
    private readonly IConfigurationBuilder _builder;
    private readonly ConcurrentDictionary<string, string> _secretCache = new();
    private IOnePasswordClient? _client;
    private bool _disposed;

    public OnePasswordConfigurationProvider(OnePasswordConfigurationSource source, IConfigurationBuilder builder)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
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
            // Scan all previous configuration sources for op:// URIs
            // This respects precedence: environment variables override op:// URIs (FR-024)
            var opUris = ScanForOpUris();

            if (opUris.Count == 0)
            {
                // No op:// URIs found, nothing to resolve
                return;
            }

            // Batch retrieve all secrets
            var resolvedSecrets = _client.GetSecretsAsync(opUris.Values).GetAwaiter().GetResult();

            // Cache resolved secrets and add to Data
            foreach (var kvp in opUris)
            {
                var configKey = kvp.Key;
                var opUri = kvp.Value;

                if (resolvedSecrets.TryGetValue(opUri, out var secretValue))
                {
                    _secretCache[configKey] = secretValue;
                    // Add to Data dictionary so this provider can serve the resolved value
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

    /// <summary>
    /// Scans all previous configuration sources for op:// URIs.
    /// </summary>
    /// <returns>Dictionary of config keys to op:// URIs that need resolution</returns>
    /// <remarks>
    /// This method builds a temporary configuration from all sources added before
    /// the OnePassword provider. It only includes keys whose FINAL VALUE (after
    /// precedence resolution) is an op:// URI. If a key has been overridden by
    /// a higher-precedence source (e.g., environment variable) with a non-op:// value,
    /// it will not be included in the result (FR-024: environment overrides secrets).
    ///
    /// Example:
    /// - appsettings.json: Database:Password = "op://vault/db/password"
    /// - environment var: Database__Password = "local-password"
    /// Result: Database:Password is NOT included (overridden by env var)
    /// </remarks>
    private Dictionary<string, string> ScanForOpUris()
    {
        var opUris = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Build temporary configuration from all previous sources
        // (excluding the OnePassword provider itself)
        var previousSources = _builder.Sources
            .Where(s => s is not OnePasswordConfigurationSource)
            .ToList();

        if (previousSources.Count == 0)
        {
            return opUris;
        }

        // Build a temporary configuration from previous sources only
        var tempBuilder = new ConfigurationBuilder();
        foreach (var source in previousSources)
        {
            tempBuilder.Add(source);
        }

        var tempConfig = tempBuilder.Build();

        // Scan all configuration keys for op:// URIs
        // AsEnumerable() gives us all key-value pairs with precedence already resolved
        foreach (var kvp in tempConfig.AsEnumerable())
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
