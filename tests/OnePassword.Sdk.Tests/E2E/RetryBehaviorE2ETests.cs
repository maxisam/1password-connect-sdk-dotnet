// E2E Tests: Retry Behavior with WireMock
// Feature: End-to-end testing of resilience policies

using System.Diagnostics;
using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OnePassword.Sdk.Client;
using OnePassword.Sdk.Exceptions;
using OnePassword.Sdk.Tests.TestHelpers;

namespace OnePassword.Sdk.Tests.E2E;

/// <summary>
/// End-to-end tests for retry behavior using WireMock.
/// </summary>
/// <remarks>
/// Validates FR-003, FR-015: Retry with exponential backoff and jitter.
/// </remarks>
public class RetryBehaviorE2ETests : IDisposable
{
    private readonly WireMockServerHelper _mockServer;

    public RetryBehaviorE2ETests()
    {
        _mockServer = new WireMockServerHelper();
    }

    [Fact]
    public async Task Request_WithTransientFailuresThenSuccess_ShouldRetryAndSucceed()
    {
        // Arrange
        var expectedVaults = new[] { new { id = "vault1", name = "Test" } };

        // Setup failures only - final success will be added separately
        _mockServer.SetupScenario(
            scenarioName: "retry-success",
            path: "/v1/vaults",
            statusCodes: new[] { 503, 503 },
            states: new[] { "attempt1", "attempt2" });

        // Add successful response for final attempt with actual data
        _mockServer.Server
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/v1/vaults")
                .UsingGet())
            .InScenario("retry-success")
            .WhenStateIs("attempt2")
            .WillSetStateTo("attempt3")
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(expectedVaults));

        var options = new OnePasswordClientOptions
        {
            ConnectServer = _mockServer.BaseUrl,
            Token = "test-token",
            MaxRetries = 3,
            RetryBaseDelay = TimeSpan.FromMilliseconds(50),
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        using var client = new OnePasswordClient(options, new HttpClientHandler());

        // Act
        var vaults = await client.ListVaultsAsync();

        // Assert
        vaults.Should().HaveCount(1);
        vaults.First().Id.Should().Be("vault1");

        // Verify all 3 attempts were made
        var requests = _mockServer.Server.LogEntries.ToList();
        requests.Should().HaveCount(3, "should have made 3 attempts (2 retries after initial failure)");
    }

    [Fact]
    public async Task Request_WithMaxRetriesExceeded_ShouldThrowAfterRetries()
    {
        // Arrange
        _mockServer.SetupResponse("/v1/vaults", 503);

        var options = new OnePasswordClientOptions
        {
            ConnectServer = _mockServer.BaseUrl,
            Token = "test-token",
            MaxRetries = 2,
            RetryBaseDelay = TimeSpan.FromMilliseconds(10),
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        using var client = new OnePasswordClient(options, new HttpClientHandler());

        // Act & Assert
        await Assert.ThrowsAsync<NetworkException>(
            async () => await client.ListVaultsAsync());

        // Verify 3 attempts total (initial + 2 retries)
        var requests = _mockServer.Server.LogEntries.ToList();
        requests.Should().HaveCount(3, "should have made 3 attempts (initial + 2 retries)");
    }

    [Fact]
    public async Task Request_WithPermanentError_ShouldNotRetry()
    {
        // Arrange
        _mockServer.SetupResponse("/v1/vaults", 401);

        var options = new OnePasswordClientOptions
        {
            ConnectServer = _mockServer.BaseUrl,
            Token = "invalid-token",
            MaxRetries = 3,
            RetryBaseDelay = TimeSpan.FromMilliseconds(10),
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        using var client = new OnePasswordClient(options, new HttpClientHandler());

        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationException>(
            async () => await client.ListVaultsAsync());

        // Verify only 1 attempt (no retries for 401)
        var requests = _mockServer.Server.LogEntries.ToList();
        requests.Should().HaveCount(1, "should not retry permanent errors like 401");
    }

    [Fact]
    public async Task Request_WithExponentialBackoff_ShouldIncreaseDelayBetweenRetries()
    {
        // Arrange
        _mockServer.SetupResponse("/v1/vaults", 503);

        var options = new OnePasswordClientOptions
        {
            ConnectServer = _mockServer.BaseUrl,
            Token = "test-token",
            MaxRetries = 3,
            RetryBaseDelay = TimeSpan.FromMilliseconds(100),
            EnableJitter = false, // Disable jitter for predictable timing
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        using var client = new OnePasswordClient(options, new HttpClientHandler());

        // Act
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await client.ListVaultsAsync();
        }
        catch
        {
            // Expected to fail
        }
        stopwatch.Stop();

        // Assert
        // With exponential backoff: 100ms, 200ms, 400ms = 700ms total delay
        // Allow tolerance for WireMock HTTP overhead and timing variations
        stopwatch.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(400, "should have delays between retries");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000, "delays should follow exponential pattern");

        var requests = _mockServer.Server.LogEntries.ToList();
        requests.Should().HaveCount(4, "should have made 4 attempts (initial + 3 retries)");
    }

    [Fact]
    public async Task Request_WithJitterEnabled_ShouldVaryRetryDelays()
    {
        // Arrange
        _mockServer.SetupResponse("/v1/vaults", 503);

        var options = new OnePasswordClientOptions
        {
            ConnectServer = _mockServer.BaseUrl,
            Token = "test-token",
            MaxRetries = 2,
            RetryBaseDelay = TimeSpan.FromMilliseconds(100),
            EnableJitter = true,
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        using var client = new OnePasswordClient(options, new HttpClientHandler());

        // Act
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await client.ListVaultsAsync();
        }
        catch
        {
            // Expected to fail
        }
        stopwatch.Stop();

        // Assert
        // With jitter enabled, delays should vary but be in reasonable range
        // Base delays: 100ms, 200ms = 300ms without jitter
        // With ±25% jitter: 225ms - 375ms expected range
        // Allow some tolerance for WireMock HTTP overhead
        stopwatch.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(150, "should have some delay");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(600, "jitter should add reasonable variance");
    }

    [Fact]
    public async Task Request_With404NotFound_ShouldNotRetry()
    {
        // Arrange
        _mockServer.SetupResponse("/v1/vaults/nonexistent", 404);

        var options = new OnePasswordClientOptions
        {
            ConnectServer = _mockServer.BaseUrl,
            Token = "test-token",
            MaxRetries = 3,
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        using var client = new OnePasswordClient(options, new HttpClientHandler());

        // Act & Assert
        await Assert.ThrowsAsync<VaultNotFoundException>(
            async () => await client.GetVaultAsync("nonexistent"));

        // Verify only 1 attempt (no retries for 404)
        var requests = _mockServer.Server.LogEntries.ToList();
        requests.Should().HaveCount(1, "should not retry 404 errors");
    }

    public void Dispose()
    {
        _mockServer?.Dispose();
    }
}
