// E2E Tests: Vault Operations with WireMock
// Feature: End-to-end testing with real HTTP

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OnePassword.Sdk.Client;
using OnePassword.Sdk.Tests.TestHelpers;

namespace OnePassword.Sdk.Tests.E2E;

/// <summary>
/// End-to-end tests for vault operations using WireMock.
/// </summary>
/// <remarks>
/// These tests verify the full HTTP stack including serialization, headers, and resilience.
/// </remarks>
public class VaultOperationsE2ETests : IDisposable
{
    private readonly WireMockServerHelper _mockServer;
    private readonly OnePasswordClient _client;

    public VaultOperationsE2ETests()
    {
        _mockServer = new WireMockServerHelper();

        var options = new OnePasswordClientOptions
        {
            ConnectServer = _mockServer.BaseUrl,
            Token = "test-token-e2e",
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        // Use internal constructor with HttpClientHandler to bypass HTTPS validation for WireMock (HTTP)
        _client = new OnePasswordClient(options, new HttpClientHandler());
    }

    [Fact]
    public async Task ListVaults_WithValidResponse_ShouldDeserializeCorrectly()
    {
        // Arrange
        var expectedVaults = new[]
        {
            new { id = "vault1", name = "Personal" },
            new { id = "vault2", name = "Work" }
        };

        _mockServer.SetupListVaults(expectedVaults);

        // Act
        var vaults = await _client.ListVaultsAsync();

        // Assert
        var vaultList = vaults.ToList();
        vaultList.Should().HaveCount(2);
        vaultList[0].Id.Should().Be("vault1");
        vaultList[0].Name.Should().Be("Personal");
        vaultList[1].Id.Should().Be("vault2");
        vaultList[1].Name.Should().Be("Work");
    }

    [Fact]
    public async Task GetVault_WithValidId_ShouldReturnVault()
    {
        // Arrange
        var expectedVault = new
        {
            id = "vault1",
            name = "Personal",
            description = "My personal vault"
        };

        _mockServer.SetupGetVault("vault1", expectedVault);

        // Act
        var vault = await _client.GetVaultAsync("vault1");

        // Assert
        vault.Should().NotBeNull();
        vault.Id.Should().Be("vault1");
        vault.Name.Should().Be("Personal");
        vault.Description.Should().Be("My personal vault");
    }

    [Fact]
    public async Task ListItems_WithValidVaultId_ShouldDeserializeItems()
    {
        // Arrange
        var expectedItems = new[]
        {
            new
            {
                id = "item1",
                title = "GitHub",
                category = "LOGIN",
                vault = new { id = "vault1" }
            },
            new
            {
                id = "item2",
                title = "AWS",
                category = "LOGIN",
                vault = new { id = "vault1" }
            }
        };

        _mockServer.SetupListItems("vault1", expectedItems);

        // Act
        var items = await _client.ListItemsAsync("vault1");

        // Assert
        var itemList = items.ToList();
        itemList.Should().HaveCount(2);
        itemList[0].Id.Should().Be("item1");
        itemList[0].Title.Should().Be("GitHub");
        itemList[1].Id.Should().Be("item2");
        itemList[1].Title.Should().Be("AWS");
    }

    [Fact]
    public async Task GetItem_WithFieldsAndSections_ShouldDeserializeComplexStructure()
    {
        // Arrange
        var expectedItem = new
        {
            id = "item1",
            title = "GitHub",
            category = "LOGIN",
            vault = new { id = "vault1" },
            fields = new[]
            {
                new
                {
                    id = "username",
                    type = "STRING",
                    label = "username",
                    value = "john.doe"
                },
                new
                {
                    id = "password",
                    type = "CONCEALED",
                    label = "password",
                    value = "secret123"
                }
            }
        };

        _mockServer.SetupGetItem("vault1", "item1", expectedItem);

        // Act
        var item = await _client.GetItemAsync("vault1", "item1");

        // Assert
        item.Should().NotBeNull();
        item.Id.Should().Be("item1");
        item.Title.Should().Be("GitHub");
        item.Fields.Should().HaveCount(2);

        var usernameField = item.Fields.First(f => f.Label == "username");
        usernameField.Value.Should().Be("john.doe");

        var passwordField = item.Fields.First(f => f.Label == "password");
        passwordField.Value.Should().Be("secret123");
    }

    [Fact]
    public async Task GetSecret_WithValidPath_ShouldExtractFieldValue()
    {
        // Arrange
        var expectedItem = new
        {
            id = "database-creds",
            title = "Database",
            category = "LOGIN",
            vault = new { id = "production" },
            fields = new[]
            {
                new
                {
                    id = "password",
                    type = "CONCEALED",
                    label = "password",
                    value = "db-secret-password"
                }
            }
        };

        _mockServer.SetupGetItem("production", "database-creds", expectedItem);

        // Act
        var secret = await _client.GetSecretAsync("production", "database-creds", "password");

        // Assert
        secret.Should().Be("db-secret-password");
    }

    [Fact]
    public async Task Client_ShouldSendAuthorizationHeader()
    {
        // Arrange
        _mockServer.SetupListVaults(Array.Empty<object>());

        // Act
        await _client.ListVaultsAsync();

        // Assert
        var requests = _mockServer.Server.LogEntries.ToList();
        requests.Should().HaveCount(1);

        var authHeader = requests[0].RequestMessage.Headers?["Authorization"];
        authHeader.Should().NotBeNull();
        authHeader.Should().ContainSingle().Which.Should().Be("Bearer test-token-e2e");
    }

    public void Dispose()
    {
        _client?.Dispose();
        _mockServer?.Dispose();
    }
}
