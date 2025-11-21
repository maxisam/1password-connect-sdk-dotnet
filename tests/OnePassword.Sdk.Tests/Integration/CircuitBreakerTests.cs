// Integration Tests: Circuit Breaker
// Feature: 002-httpclient-factory-polly

using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OnePassword.Sdk.Client;
using OnePassword.Sdk.Exceptions;
using OnePassword.Sdk.Tests.TestHelpers;
using Polly.CircuitBreaker;

namespace OnePassword.Sdk.Tests.Integration;

/// <summary>
/// Integration tests for circuit breaker behavior.
/// </summary>
/// <remarks>
/// Verifies FR-004, FR-014: Circuit breaker pattern with configurable thresholds.
/// </remarks>
public class CircuitBreakerTests
{
    [Fact]
    public async Task CircuitBreaker_WithCustomFailureThreshold_ShouldHonorConfiguration()
    {
        // Arrange
        // With MaxRetries=1, each request attempts twice (original + 1 retry)
        // To trigger circuit breaker with threshold=3, we need 6 failures (3 requests × 2 attempts)
        var handler = new SimulatedFailureHttpMessageHandler()
            .RespondWith(HttpStatusCode.ServiceUnavailable) // Request 1, attempt 1
            .RespondWith(HttpStatusCode.ServiceUnavailable) // Request 1, attempt 2 (retry)
            .RespondWith(HttpStatusCode.ServiceUnavailable) // Request 2, attempt 1
            .RespondWith(HttpStatusCode.ServiceUnavailable) // Request 2, attempt 2 (retry)
            .RespondWith(HttpStatusCode.ServiceUnavailable) // Request 3, attempt 1
            .RespondWith(HttpStatusCode.ServiceUnavailable) // Request 3, attempt 2 (retry)
            .RespondWith(HttpStatusCode.OK); // Should not reach this - circuit should be open

        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            CircuitBreakerFailureThreshold = 3, // Custom: 3 failures instead of default 5
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(2),
            CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(10),
            MaxRetries = 1, // Each request will be attempted twice (original + 1 retry)
            RetryBaseDelay = TimeSpan.FromMilliseconds(1), // Very short delay
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        using var client = new OnePasswordClient(options, handler);

        // Act & Assert - Make 3 requests, each will be attempted twice
        for (int i = 0; i < 3; i++)
        {
            await Assert.ThrowsAsync<NetworkException>(
                async () => await client.ListVaultsAsync());
        }

        // Circuit should now be open after 6 attempts (3 requests × 2 attempts each)
        handler.RequestCount.Should().Be(6, "all 3 requests with retries should have executed");

        // Next request should fail immediately due to open circuit (BrokenCircuitException)
        await Assert.ThrowsAsync<BrokenCircuitException>(
            async () => await client.ListVaultsAsync());

        // Request count should still be 6 (circuit blocked the 4th request)
        handler.RequestCount.Should().Be(6, "open circuit should prevent request from reaching handler");
    }

    [Fact]
    public async Task CircuitBreaker_StateTransitions_ShouldFollowClosedOpenHalfOpenClosed()
    {
        // Arrange
        // With MaxRetries=1, each request attempts twice
        // CircuitBreakerFailureThreshold=2 means we need 4 failures (2 requests × 2 attempts)
        var handler = new SimulatedFailureHttpMessageHandler()
            .RespondWith(HttpStatusCode.ServiceUnavailable) // Request 1, attempt 1
            .RespondWith(HttpStatusCode.ServiceUnavailable) // Request 1, attempt 2 (retry)
            .RespondWith(HttpStatusCode.ServiceUnavailable) // Request 2, attempt 1
            .RespondWith(HttpStatusCode.ServiceUnavailable) // Request 2, attempt 2 (retry) - circuit opens
            .RespondWith(HttpStatusCode.OK); // Request 3 (after break): Success (closes circuit)

        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            CircuitBreakerFailureThreshold = 2,
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(1),
            CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(10),
            MaxRetries = 1,
            RetryBaseDelay = TimeSpan.FromMilliseconds(1),
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        using var client = new OnePasswordClient(options, handler);

        // Act & Assert
        // State 1: Closed - First 2 requests (4 attempts) should open the circuit
        await Assert.ThrowsAsync<NetworkException>(async () => await client.ListVaultsAsync());
        await Assert.ThrowsAsync<NetworkException>(async () => await client.ListVaultsAsync());

        handler.RequestCount.Should().Be(4, "circuit should have opened after 2 requests (4 attempts)");

        // State 2: Open - Immediate failure without hitting handler
        await Assert.ThrowsAsync<BrokenCircuitException>(async () => await client.ListVaultsAsync());
        handler.RequestCount.Should().Be(4, "open circuit should block requests");

        // State 3: Wait for break duration to transition to Half-Open
        await Task.Delay(TimeSpan.FromSeconds(1.1)); // Slightly longer than break duration

        // State 4: Half-Open → Closed - Next request should succeed and close circuit
        var vaults = await client.ListVaultsAsync();
        handler.RequestCount.Should().Be(5, "half-open circuit allowed test request");
        vaults.Should().NotBeNull("circuit should be closed after successful test");
    }

    [Fact]
    public async Task CircuitBreaker_WithCustomBreakDuration_ShouldRespectDuration()
    {
        // Arrange
        // With MaxRetries=1, each request attempts twice
        // CircuitBreakerFailureThreshold=2 means we need 4 failures (2 requests × 2 attempts)
        var handler = new SimulatedFailureHttpMessageHandler()
            .RespondWith(HttpStatusCode.ServiceUnavailable) // Request 1, attempt 1
            .RespondWith(HttpStatusCode.ServiceUnavailable) // Request 1, attempt 2 (retry)
            .RespondWith(HttpStatusCode.ServiceUnavailable) // Request 2, attempt 1
            .RespondWith(HttpStatusCode.ServiceUnavailable) // Request 2, attempt 2 (retry) - circuit opens
            .RespondWith(HttpStatusCode.OK); // Request 3 (after 3s break): Success

        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            CircuitBreakerFailureThreshold = 2,
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(3), // Custom: 3 seconds instead of default 30
            CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(10),
            MaxRetries = 1,
            RetryBaseDelay = TimeSpan.FromMilliseconds(1),
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        using var client = new OnePasswordClient(options, handler);

        // Act & Assert
        // Trigger circuit open with 2 requests (4 attempts)
        await Assert.ThrowsAsync<NetworkException>(async () => await client.ListVaultsAsync());
        await Assert.ThrowsAsync<NetworkException>(async () => await client.ListVaultsAsync());

        handler.RequestCount.Should().Be(4, "circuit opened after 2 requests (4 attempts)");

        // Verify circuit stays open (before break duration expires)
        await Task.Delay(TimeSpan.FromSeconds(1)); // Wait 1 second (less than 3s break duration)
        await Assert.ThrowsAsync<BrokenCircuitException>(async () => await client.ListVaultsAsync());
        handler.RequestCount.Should().Be(4, "circuit should still be open after 1 second");

        // Wait for break duration to expire
        await Task.Delay(TimeSpan.FromSeconds(2.2)); // Wait additional 2.2s (total ~3.2s > 3s break duration)

        // Circuit should transition to half-open and allow test request
        var vaults = await client.ListVaultsAsync();
        handler.RequestCount.Should().Be(5, "circuit transitioned to half-open after 3 seconds");
        vaults.Should().NotBeNull("test request succeeded");
    }
}
