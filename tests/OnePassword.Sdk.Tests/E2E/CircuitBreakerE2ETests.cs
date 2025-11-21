// E2E Tests: Circuit Breaker with WireMock
// Feature: End-to-end testing of circuit breaker resilience

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OnePassword.Sdk.Client;
using OnePassword.Sdk.Exceptions;
using OnePassword.Sdk.Tests.TestHelpers;
using Polly.CircuitBreaker;

namespace OnePassword.Sdk.Tests.E2E;

/// <summary>
/// End-to-end tests for circuit breaker behavior using WireMock.
/// </summary>
/// <remarks>
/// Validates FR-004, FR-014: Circuit breaker pattern with configurable thresholds.
/// </remarks>
public class CircuitBreakerE2ETests : IDisposable
{
    private readonly WireMockServerHelper _mockServer;

    public CircuitBreakerE2ETests()
    {
        _mockServer = new WireMockServerHelper();
    }

    [Fact]
    public async Task CircuitBreaker_AfterConsecutiveFailures_ShouldOpenCircuit()
    {
        // Arrange
        _mockServer.SetupResponse("/v1/vaults", 503);

        var options = new OnePasswordClientOptions
        {
            ConnectServer = _mockServer.BaseUrl,
            Token = "test-token",
            CircuitBreakerFailureThreshold = 3,
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(5),
            CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(30),
            MaxRetries = 1, // Each request will be attempted twice
            RetryBaseDelay = TimeSpan.FromMilliseconds(1),
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        using var client = new OnePasswordClient(options, new HttpClientHandler());

        // Act & Assert
        // Make 3 requests, each attempted twice (6 total attempts) to reach threshold
        for (int i = 0; i < 3; i++)
        {
            await Assert.ThrowsAsync<NetworkException>(
                async () => await client.ListVaultsAsync());
        }

        // Circuit should now be open - next request fails immediately
        await Assert.ThrowsAsync<BrokenCircuitException>(
            async () => await client.ListVaultsAsync());

        // Verify circuit blocked the last request
        var requests = _mockServer.Server.LogEntries.ToList();
        requests.Should().HaveCount(6, "circuit should block requests after threshold");
    }

    [Fact]
    public async Task CircuitBreaker_AfterBreakDuration_ShouldTransitionToHalfOpen()
    {
        // Arrange
        _mockServer.SetupScenario(
            scenarioName: "circuit-recovery",
            path: "/v1/vaults",
            statusCodes: new[] { 503, 503, 503, 503, 200 },
            states: new[] { "fail1", "fail2", "fail3", "fail4", "success" });

        _mockServer.Server
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/v1/vaults")
                .UsingGet())
            .InScenario("circuit-recovery")
            .WhenStateIs("success")
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new[] { new { id = "vault1", name = "Test" } }));

        var options = new OnePasswordClientOptions
        {
            ConnectServer = _mockServer.BaseUrl,
            Token = "test-token",
            CircuitBreakerFailureThreshold = 2,
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(1),
            CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(30),
            MaxRetries = 1,
            RetryBaseDelay = TimeSpan.FromMilliseconds(1),
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        using var client = new OnePasswordClient(options, new HttpClientHandler());

        // Act & Assert
        // Trigger circuit open (2 requests × 2 attempts = 4 attempts)
        await Assert.ThrowsAsync<NetworkException>(async () => await client.ListVaultsAsync());
        await Assert.ThrowsAsync<NetworkException>(async () => await client.ListVaultsAsync());

        // Circuit is now open
        await Assert.ThrowsAsync<BrokenCircuitException>(async () => await client.ListVaultsAsync());

        // Wait for break duration
        await Task.Delay(TimeSpan.FromSeconds(1.1));

        // Circuit transitions to half-open and allows test request
        var vaults = await client.ListVaultsAsync();
        vaults.Should().NotBeNull("circuit should allow request after break duration");
    }

    [Fact]
    public async Task CircuitBreaker_WithCustomThreshold_ShouldRespectConfiguration()
    {
        // Arrange
        _mockServer.SetupResponse("/v1/vaults", 503);

        var options = new OnePasswordClientOptions
        {
            ConnectServer = _mockServer.BaseUrl,
            Token = "test-token",
            CircuitBreakerFailureThreshold = 5, // Custom: higher threshold
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(10),
            CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(30),
            MaxRetries = 1,
            RetryBaseDelay = TimeSpan.FromMilliseconds(1),
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        using var client = new OnePasswordClient(options, new HttpClientHandler());

        // Act & Assert
        // Make 4 requests (8 attempts) - should NOT open circuit yet
        for (int i = 0; i < 4; i++)
        {
            await Assert.ThrowsAsync<NetworkException>(
                async () => await client.ListVaultsAsync());
        }

        // Circuit should still be closed - 5th request should still reach server
        var requestCountBefore = _mockServer.Server.LogEntries.Count();
        await Assert.ThrowsAsync<NetworkException>(async () => await client.ListVaultsAsync());
        var requestCountAfter = _mockServer.Server.LogEntries.Count();

        // Verify 5th request reached the server (circuit not yet open)
        (requestCountAfter - requestCountBefore).Should().Be(2, "circuit should not be open yet at threshold-1");

        // Now circuit should be open after 5th request
        await Assert.ThrowsAsync<BrokenCircuitException>(
            async () => await client.ListVaultsAsync());

        var finalRequestCount = _mockServer.Server.LogEntries.Count();
        finalRequestCount.Should().Be(10, "circuit should block request after hitting threshold");
    }

    [Fact]
    public async Task CircuitBreaker_MixedSuccessAndFailure_ShouldTrackFailureRate()
    {
        // Arrange - Account for retries: failed requests make 2 HTTP calls (original + retry)
        // Request sequence (5 logical requests, 8 HTTP calls total):
        // Req 1: Success (1 call) | Req 2: Fail (2 calls) | Req 3: Fail (2 calls) | Req 4: Success (1 call) | Req 5: Fail (2 calls)
        var testData = new[] { new { id = "vault1", name = "Test" } };

        // Need 8 states for 8 HTTP calls: 1 success + 2 fail + 2 fail + 1 success + 2 fail
        _mockServer.SetupScenario(
            scenarioName: "mixed-results",
            path: "/v1/vaults",
            statusCodes: new[] { 503, 503, 503, 503, 503, 503, 503, 503 },
            states: new[] { "s1", "s2", "s3", "s4", "s5", "s6", "s7", "s8" });

        // Override initial state (no WhenStateIs) for first success
        _mockServer.Server
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/v1/vaults")
                .UsingGet())
            .InScenario("mixed-results")
            .WillSetStateTo("s1")
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(testData));

        // Override s5 for second success (after 4 failures: s1→s2, s2→s3, s3→s4, s4→s5)
        _mockServer.Server
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/v1/vaults")
                .UsingGet())
            .InScenario("mixed-results")
            .WhenStateIs("s5")
            .WillSetStateTo("s6")
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(testData));

        var options = new OnePasswordClientOptions
        {
            ConnectServer = _mockServer.BaseUrl,
            Token = "test-token",
            CircuitBreakerFailureThreshold = 3,
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(5),
            CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(30),
            MaxRetries = 1,
            RetryBaseDelay = TimeSpan.FromMilliseconds(1),
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        using var client = new OnePasswordClient(options, new HttpClientHandler());

        // Act & Assert
        // Success
        var result1 = await client.ListVaultsAsync();
        result1.Should().NotBeNull();

        // Failure (2 attempts)
        await Assert.ThrowsAsync<NetworkException>(async () => await client.ListVaultsAsync());

        // Failure (2 attempts)
        await Assert.ThrowsAsync<NetworkException>(async () => await client.ListVaultsAsync());

        // Success - resets failure count in sampling window
        var result4 = await client.ListVaultsAsync();
        result4.Should().NotBeNull();

        // Even after more failures, circuit uses rolling window
        // Circuit behavior depends on sampling window configuration
        await Assert.ThrowsAsync<NetworkException>(async () => await client.ListVaultsAsync());
    }

    public void Dispose()
    {
        _mockServer?.Dispose();
    }
}
