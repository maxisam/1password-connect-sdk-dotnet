// Test Helpers: WireMock Server Setup
// Feature: E2E Testing with WireMock

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace OnePassword.Sdk.Tests.TestHelpers;

/// <summary>
/// Helper class for setting up WireMock server for E2E tests.
/// </summary>
public class WireMockServerHelper : IDisposable
{
    private readonly WireMockServer _server;

    public WireMockServerHelper()
    {
        _server = WireMockServer.Start(new WireMockServerSettings
        {
            Port = 0, // Use random available port
            UseSSL = false // Use HTTP for testing (SSL not required for localhost)
        });
    }

    /// <summary>
    /// Gets the base URL of the WireMock server.
    /// </summary>
    public string BaseUrl => _server.Urls[0];

    /// <summary>
    /// Gets the underlying WireMock server instance.
    /// </summary>
    public WireMockServer Server => _server;

    /// <summary>
    /// Sets up a mock response for listing vaults.
    /// </summary>
    public WireMockServerHelper SetupListVaults(params object[] vaults)
    {
        _server
            .Given(Request.Create()
                .WithPath("/v1/vaults")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(vaults));

        return this;
    }

    /// <summary>
    /// Sets up a mock response for getting a specific vault.
    /// </summary>
    public WireMockServerHelper SetupGetVault(string vaultId, object vault)
    {
        _server
            .Given(Request.Create()
                .WithPath($"/v1/vaults/{vaultId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(vault));

        return this;
    }

    /// <summary>
    /// Sets up a mock response for listing items in a vault.
    /// </summary>
    public WireMockServerHelper SetupListItems(string vaultId, params object[] items)
    {
        _server
            .Given(Request.Create()
                .WithPath($"/v1/vaults/{vaultId}/items")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(items));

        return this;
    }

    /// <summary>
    /// Sets up a mock response for getting a specific item.
    /// </summary>
    public WireMockServerHelper SetupGetItem(string vaultId, string itemId, object item)
    {
        _server
            .Given(Request.Create()
                .WithPath($"/v1/vaults/{vaultId}/items/{itemId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(item));

        return this;
    }

    /// <summary>
    /// Sets up a mock response that returns a specific status code with optional delay.
    /// </summary>
    public WireMockServerHelper SetupResponse(string path, int statusCode, TimeSpan? delay = null)
    {
        var response = Response.Create().WithStatusCode(statusCode);

        if (delay.HasValue)
        {
            response = response.WithDelay(delay.Value);
        }

        _server
            .Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(response);

        return this;
    }

    /// <summary>
    /// Sets up a scenario for testing state transitions (e.g., circuit breaker).
    /// </summary>
    public WireMockServerHelper SetupScenario(string scenarioName, string path,
        int[] statusCodes, string[] states)
    {
        if (statusCodes.Length != states.Length)
        {
            throw new ArgumentException("Status codes and states must have the same length");
        }

        for (int i = 0; i < statusCodes.Length; i++)
        {
            var request = Request.Create()
                .WithPath(path)
                .UsingGet();

            var response = Response.Create()
                .WithStatusCode(statusCodes[i]);

            // Add empty JSON array body for successful responses to prevent deserialization errors
            if (statusCodes[i] == 200)
            {
                response = response
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("[]");
            }

            if (i == 0)
            {
                // First state - no required state, set to state[0]
                _server.Given(request)
                    .InScenario(scenarioName)
                    .WillSetStateTo(states[0])
                    .RespondWith(response);
            }
            else
            {
                // Subsequent states - require previous state, set to current state
                _server.Given(request)
                    .InScenario(scenarioName)
                    .WhenStateIs(states[i - 1])
                    .WillSetStateTo(states[i])
                    .RespondWith(response);
            }
        }

        return this;
    }

    /// <summary>
    /// Resets all configured mappings.
    /// </summary>
    public void Reset()
    {
        _server.Reset();
    }

    public void Dispose()
    {
        _server?.Stop();
        _server?.Dispose();
    }
}
