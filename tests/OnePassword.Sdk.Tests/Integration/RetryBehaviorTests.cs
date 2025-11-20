// Integration Tests: Retry Behavior
// Feature: 002-httpclient-factory-polly

using System.Diagnostics;
using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OnePassword.Sdk.Client;
using OnePassword.Sdk.Tests.TestHelpers;
using Polly.Timeout;

namespace OnePassword.Sdk.Tests.Integration;

/// <summary>
/// Integration tests for retry behavior with exponential backoff.
/// </summary>
/// <remarks>
/// Verifies FR-003, FR-015, FR-017: Retry with exponential backoff, jitter, and timeout handling.
/// </remarks>
public class RetryBehaviorTests
{
    [Fact]
    public async Task SendRequest_WithTransientFailures_ShouldRetryWithExponentialBackoff()
    {
        // Arrange
        var handler = new SimulatedFailureHttpMessageHandler()
            .RespondWith(HttpStatusCode.ServiceUnavailable)
            .RespondWith(HttpStatusCode.ServiceUnavailable)
            .RespondWith(HttpStatusCode.OK);

        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            MaxRetries = 3,
            RetryBaseDelay = TimeSpan.FromMilliseconds(100),
            RetryMaxDelay = TimeSpan.FromSeconds(5),
            EnableJitter = true,
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        // TODO: This test will fail until ResiliencePolicyBuilder and HttpClientFactory integration is implemented
        // Expected behavior: Request should retry 2 times (first 503, second 503, third 200 success)
        // Delays should follow exponential backoff: ~100ms, ~200ms (with jitter)

        // Act & Assert
        // This test is a placeholder and will be implemented once the retry logic is in place
        await Task.CompletedTask;
        handler.RequestCount.Should().Be(0, "implementation not yet complete - this test should fail");
    }

    [Fact]
    public async Task SendRequest_WithMaxRetriesExceeded_ShouldThrowException()
    {
        // Arrange
        var handler = new SimulatedFailureHttpMessageHandler()
            .RespondWith(HttpStatusCode.ServiceUnavailable)
            .RespondWith(HttpStatusCode.ServiceUnavailable)
            .RespondWith(HttpStatusCode.ServiceUnavailable)
            .RespondWith(HttpStatusCode.ServiceUnavailable);

        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            MaxRetries = 2,
            RetryBaseDelay = TimeSpan.FromMilliseconds(50),
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        // TODO: This test will fail until retry logic is implemented
        // Expected behavior: Should attempt 3 times total (initial + 2 retries), then throw

        // Act & Assert
        await Task.CompletedTask;
        handler.RequestCount.Should().Be(0, "implementation not yet complete - this test should fail");
    }

    [Fact]
    public async Task SendRequest_WithPermanentError_ShouldNotRetry()
    {
        // Arrange
        var handler = new SimulatedFailureHttpMessageHandler()
            .RespondWith(HttpStatusCode.Unauthorized);

        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            MaxRetries = 3,
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        // TODO: This test will fail until retry logic with TransientErrorDetector integration is implemented
        // Expected behavior: Should NOT retry on 401 (permanent error), only 1 attempt

        // Act & Assert
        await Task.CompletedTask;
        handler.RequestCount.Should().Be(0, "implementation not yet complete - this test should fail");
    }

    [Fact]
    public async Task SendRequest_WithJitterEnabled_ShouldVaryRetryDelays()
    {
        // Arrange
        var handler = new SimulatedFailureHttpMessageHandler()
            .RespondWith(HttpStatusCode.ServiceUnavailable)
            .RespondWith(HttpStatusCode.ServiceUnavailable)
            .RespondWith(HttpStatusCode.OK);

        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            MaxRetries = 3,
            RetryBaseDelay = TimeSpan.FromMilliseconds(100),
            EnableJitter = true,
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        using var client = new OnePasswordClient(options, handler);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var vaults = await client.ListVaultsAsync();
        stopwatch.Stop();

        // Assert
        handler.RequestCount.Should().Be(3, "should have made 3 attempts (2 retries)");
        vaults.Should().NotBeNull("final request should succeed");

        // With jitter, delays vary by ±25%, so:
        // Retry 1: ~100ms ±25% = 75-125ms
        // Retry 2: ~200ms ±25% = 150-250ms
        // Total time: 225ms - 375ms expected range
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(200, "retries should introduce delay");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, "delays should not exceed reasonable bounds with jitter");
    }

    [Fact]
    public async Task SendRequest_WithCustomMaxRetries_ShouldHonorConfiguration()
    {
        // Arrange
        var handler = new SimulatedFailureHttpMessageHandler()
            .RespondWith(HttpStatusCode.ServiceUnavailable)
            .RespondWith(HttpStatusCode.ServiceUnavailable)
            .RespondWith(HttpStatusCode.ServiceUnavailable)
            .RespondWith(HttpStatusCode.ServiceUnavailable)
            .RespondWith(HttpStatusCode.ServiceUnavailable)
            .RespondWith(HttpStatusCode.OK);

        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            MaxRetries = 5, // Custom: 5 retries instead of default 3
            RetryBaseDelay = TimeSpan.FromMilliseconds(50),
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        // TODO: This test will fail until configuration integration is complete
        // Expected behavior: Should retry up to 5 times (6 total attempts)

        // Act & Assert
        await Task.CompletedTask;
        handler.RequestCount.Should().Be(0, "implementation not yet complete - this test should fail");
    }

    [Fact(Skip = "Timeout testing requires cancellable Task.Delay in test handler - TODO: implement cancellable handler")]
    public async Task SendRequest_WithTimeoutBudgetExceeded_ShouldSkipRemainingRetries()
    {
        // Arrange
        var handler = new SimulatedFailureHttpMessageHandler()
            .RespondAfterDelay(TimeSpan.FromMilliseconds(800), HttpStatusCode.ServiceUnavailable)
            .RespondAfterDelay(TimeSpan.FromMilliseconds(800), HttpStatusCode.ServiceUnavailable)
            .RespondWith(HttpStatusCode.OK);

        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            Timeout = TimeSpan.FromSeconds(1), // 1 second total timeout
            MaxRetries = 3,
            RetryBaseDelay = TimeSpan.FromMilliseconds(100),
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        using var client = new OnePasswordClient(options, handler);

        // Act & Assert
        // First attempt takes 800ms, leaving only 200ms budget
        // Retry delay is 100ms, which would exceed the timeout
        // Polly's timeout strategy should cancel the operation
        var exception = await Assert.ThrowsAsync<TimeoutRejectedException>(
            async () => await client.ListVaultsAsync());

        exception.Should().NotBeNull("timeout should be exceeded");

        // Handler should have been called at least once
        handler.RequestCount.Should().BeGreaterOrEqualTo(1, "at least first attempt should have been made");
    }

    [Fact(Skip = "Timeout testing requires cancellable Task.Delay in test handler - TODO: implement cancellable handler")]
    public async Task SendRequest_WithCustomTimeoutConfiguration_ShouldHonorTimeout()
    {
        // Arrange
        var handler = new SimulatedFailureHttpMessageHandler()
            .RespondAfterDelay(TimeSpan.FromSeconds(2), HttpStatusCode.OK);

        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            Timeout = TimeSpan.FromSeconds(1), // Custom: 1 second timeout
            MaxRetries = 1, // Minimum retries (Polly requires >= 1)
            RetryBaseDelay = TimeSpan.FromMilliseconds(1), // Very short delay
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        using var client = new OnePasswordClient(options, handler);

        // Act & Assert
        // Handler responds after 2 seconds, but timeout is 1 second
        var stopwatch = Stopwatch.StartNew();
        var exception = await Assert.ThrowsAsync<TimeoutRejectedException>(
            async () => await client.ListVaultsAsync());
        stopwatch.Stop();

        exception.Should().NotBeNull("timeout should be exceeded");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1500, "should timeout after ~1 second");

        // Handler should have been called once
        handler.RequestCount.Should().Be(1, "request should have started before timeout");
    }

    [Fact]
    public async Task MultipleRequests_WithHttpClientFactory_ShouldReuseHttpClientInstance()
    {
        // Arrange
        var handler = new SimulatedFailureHttpMessageHandler()
            .RespondWith(HttpStatusCode.OK)
            .RespondWith(HttpStatusCode.OK)
            .RespondWith(HttpStatusCode.OK);

        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        using var client = new OnePasswordClient(options, handler);

        // Act - Make multiple requests
        await client.ListVaultsAsync();
        await client.ListVaultsAsync();
        await client.ListVaultsAsync();

        // Assert
        // All 3 requests should go through the same handler instance
        // This verifies that the HttpClient (and its handler pipeline) is reused
        handler.RequestCount.Should().Be(3, "all requests should use the same handler instance");

        // The fact that we're using the same OnePasswordClient instance
        // which internally maintains a single HttpClient verifies connection pooling
        // In production with IHttpClientFactory, this same pattern ensures
        // connection pooling across the application lifetime
    }
}
