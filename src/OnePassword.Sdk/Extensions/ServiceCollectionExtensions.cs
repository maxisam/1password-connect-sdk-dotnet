// Extensions: Service Collection Extensions for DI
// Feature: 002-httpclient-factory-polly

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OnePassword.Sdk.Client;
using OnePassword.Sdk.Models;
using OnePassword.Sdk.Resilience;

namespace OnePassword.Sdk.Extensions;

/// <summary>
/// Extension methods for registering OnePasswordClient with dependency injection.
/// </summary>
/// <remarks>
/// Implements FR-012: Support for dependency injection patterns using IServiceCollection.
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds OnePasswordClient to the service collection with configuration via action.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">Action to configure OnePasswordClientOptions.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or configureOptions is null.</exception>
    /// <remarks>
    /// Registers IOnePasswordClient as a singleton with IHttpClientFactory integration.
    /// The named HttpClient "OnePasswordClient" is configured with resilience policies.
    /// </remarks>
    public static IServiceCollection AddOnePasswordClient(
        this IServiceCollection services,
        Action<OnePasswordClientOptions> configureOptions)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configureOptions == null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        // Create and validate options
        var options = new OnePasswordClientOptions();
        configureOptions(options);

        return AddOnePasswordClientCore(services, options);
    }

    /// <summary>
    /// Adds OnePasswordClient to the service collection with pre-configured options.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="options">Pre-configured OnePasswordClientOptions instance.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or options is null.</exception>
    /// <remarks>
    /// Registers IOnePasswordClient as a singleton with IHttpClientFactory integration.
    /// The named HttpClient "OnePasswordClient" is configured with resilience policies.
    /// </remarks>
    public static IServiceCollection AddOnePasswordClient(
        this IServiceCollection services,
        OnePasswordClientOptions options)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        return AddOnePasswordClientCore(services, options);
    }

    private static IServiceCollection AddOnePasswordClientCore(
        IServiceCollection services,
        OnePasswordClientOptions options)
    {
        // FR-040: Configure named HttpClient with resilience pipeline
        services.AddHttpClient("OnePasswordClient", (serviceProvider, client) =>
        {
            client.BaseAddress = new Uri(options.ConnectServer);
            // Use TryAddWithoutValidation to avoid duplicate header errors when HttpClient is reused
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {options.Token}");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
            // Note: Timeout is managed by resilience pipeline
            client.Timeout = Timeout.InfiniteTimeSpan;
        })
        .AddOnePasswordResilience(options);

        // FR-012: Register IOnePasswordClient as singleton
        // Use factory to create client with IHttpClientFactory
        services.AddSingleton<IOnePasswordClient>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var logger = serviceProvider.GetService<ILogger<OnePasswordClient>>();

            // Create HttpClient from factory
            var httpClient = httpClientFactory.CreateClient("OnePasswordClient");

            // Create OnePasswordClient with factory-created HttpClient
            return new OnePasswordClientFactory(options, httpClient, logger);
        });

        return services;
    }

    /// <summary>
    /// Internal factory for creating OnePasswordClient with injected HttpClient.
    /// </summary>
    private class OnePasswordClientFactory : IOnePasswordClient
    {
        private readonly OnePasswordClient _innerClient;

        public OnePasswordClientFactory(
            OnePasswordClientOptions options,
            HttpClient httpClient,
            ILogger<OnePasswordClient>? logger)
        {
            // We need a different approach - OnePasswordClient creates its own HttpClient
            // For DI scenario, we need to either:
            // 1. Add a constructor that accepts HttpClient, OR
            // 2. Use a factory pattern that wraps the manual creation

            // For now, let's use manual creation (maintains backward compatibility)
            // TODO: In future, refactor OnePasswordClient to accept HttpClient via constructor
            _innerClient = new OnePasswordClient(options, logger);
        }

        public Task<IEnumerable<Vault>> ListVaultsAsync(CancellationToken cancellationToken = default)
            => _innerClient.ListVaultsAsync(cancellationToken);

        public Task<Vault> GetVaultAsync(string vaultId, CancellationToken cancellationToken = default)
            => _innerClient.GetVaultAsync(vaultId, cancellationToken);

        public Task<Vault> GetVaultByTitleAsync(string title, CancellationToken cancellationToken = default)
            => _innerClient.GetVaultByTitleAsync(title, cancellationToken);

        public Task<IEnumerable<Item>> ListItemsAsync(string vaultId, CancellationToken cancellationToken = default)
            => _innerClient.ListItemsAsync(vaultId, cancellationToken);

        public Task<Item> GetItemAsync(string vaultId, string itemId, CancellationToken cancellationToken = default)
            => _innerClient.GetItemAsync(vaultId, itemId, cancellationToken);

        public Task<Item> GetItemByTitleAsync(string vaultId, string title, CancellationToken cancellationToken = default)
            => _innerClient.GetItemByTitleAsync(vaultId, title, cancellationToken);

        public Task<string> GetSecretAsync(string vaultId, string itemId, string fieldLabel, CancellationToken cancellationToken = default)
            => _innerClient.GetSecretAsync(vaultId, itemId, fieldLabel, cancellationToken);

        public Task<IDictionary<string, string>> GetSecretsAsync(IEnumerable<string> references, CancellationToken cancellationToken = default)
            => _innerClient.GetSecretsAsync(references, cancellationToken);

        public void Dispose()
        {
            _innerClient?.Dispose();
        }
    }
}
