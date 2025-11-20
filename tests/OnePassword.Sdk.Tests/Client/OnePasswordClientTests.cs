// Unit Tests: OnePasswordClient
// Feature: 001-onepassword-sdk

using FluentAssertions;
using Moq;
using Moq.Protected;
using OnePassword.Sdk.Client;
using OnePassword.Sdk.Exceptions;
using OnePassword.Sdk.Models;
using System.Net;
using System.Text.Json;

namespace OnePassword.Sdk.Tests.Client;

/// <summary>
/// Unit tests for OnePasswordClient initialization and configuration.
/// </summary>
public class OnePasswordClientTests
{
    [Fact]
    public void Constructor_Should_Throw_When_Options_Null()
    {
        // Act
        Action act = () => new OnePasswordClient(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_Should_Throw_When_ConnectServer_Not_HTTPS()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "http://localhost:8080", // HTTP not allowed
            Token = "test-token"
        };

        // Act
        Action act = () => new OnePasswordClient(options);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*HTTPS*");
    }

    [Fact]
    public void Constructor_Should_Throw_When_Token_Empty()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "" // Empty token
        };

        // Act
        Action act = () => new OnePasswordClient(options);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Token*");
    }

    [Fact]
    public void Constructor_Should_Succeed_With_Valid_Options()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://connect.example.com",
            Token = "valid-token-123"
        };

        // Act
        using var client = new OnePasswordClient(options);

        // Assert
        client.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_Should_Not_Throw()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://connect.example.com",
            Token = "valid-token-123"
        };

        var client = new OnePasswordClient(options);

        // Act
        Action act = () => client.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_Can_Be_Called_Multiple_Times()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://connect.example.com",
            Token = "valid-token-123"
        };

        var client = new OnePasswordClient(options);

        // Act
        client.Dispose();
        Action act = () => client.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetVaultAsync_Should_Throw_When_VaultId_Invalid(string? vaultId)
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://connect.example.com",
            Token = "test-token"
        };

        using var client = new OnePasswordClient(options);

        // Act
        Func<Task> act = async () => await client.GetVaultAsync(vaultId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetItemAsync_Should_Throw_When_VaultId_Invalid(string? vaultId)
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://connect.example.com",
            Token = "test-token"
        };

        using var client = new OnePasswordClient(options);

        // Act
        Func<Task> act = async () => await client.GetItemAsync(vaultId!, "item-123");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetItemAsync_Should_Throw_When_ItemId_Invalid(string? itemId)
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://connect.example.com",
            Token = "test-token"
        };

        using var client = new OnePasswordClient(options);

        // Act
        Func<Task> act = async () => await client.GetItemAsync("vault-123", itemId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSecretAsync_Should_Throw_When_FieldLabel_Invalid(string? fieldLabel)
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://connect.example.com",
            Token = "test-token"
        };

        using var client = new OnePasswordClient(options);

        // Act
        Func<Task> act = async () => await client.GetSecretAsync("vault-123", "item-456", fieldLabel!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetSecretsAsync_Should_Throw_When_References_Null()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://connect.example.com",
            Token = "test-token"
        };

        using var client = new OnePasswordClient(options);

        // Act
        Func<Task> act = async () => await client.GetSecretsAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetSecretsAsync_Should_Throw_When_References_Empty()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://connect.example.com",
            Token = "test-token"
        };

        using var client = new OnePasswordClient(options);

        // Act
        Func<Task> act = async () => await client.GetSecretsAsync(Array.Empty<string>());

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetSecretsAsync_Should_Throw_When_Batch_Size_Exceeded()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://connect.example.com",
            Token = "test-token"
        };

        using var client = new OnePasswordClient(options);

        var references = Enumerable.Range(1, 101)
            .Select(i => $"op://vault/item{i}/field")
            .ToList();

        // Act
        Func<Task> act = async () => await client.GetSecretsAsync(references);

        // Assert
        await act.Should().ThrowAsync<BatchSizeExceededException>()
            .Where(ex => ex.RequestedCount == 101 && ex.MaximumAllowed == 100);
    }

    [Fact]
    public async Task GetSecretsAsync_Should_Throw_When_URI_Malformed()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://connect.example.com",
            Token = "test-token"
        };

        using var client = new OnePasswordClient(options);

        var references = new[] { "not-an-op-uri" };

        // Act
        Func<Task> act = async () => await client.GetSecretsAsync(references);

        // Assert
        await act.Should().ThrowAsync<MalformedUriException>();
    }
}
