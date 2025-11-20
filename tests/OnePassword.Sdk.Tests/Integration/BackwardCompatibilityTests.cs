// Integration Tests: Backward Compatibility
// Feature: 002-httpclient-factory-polly

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OnePassword.Sdk.Client;

namespace OnePassword.Sdk.Tests.Integration;

/// <summary>
/// Integration tests for backward compatibility.
/// </summary>
/// <remarks>
/// Verifies SC-001, FR-006: All existing functionality continues to work without modification.
/// </remarks>
public class BackwardCompatibilityTests
{
    [Fact]
    public void OnePasswordClient_WithManualInstantiation_ShouldStillWork()
    {
        // Arrange & Act - Traditional manual instantiation (backward compatible)
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            Timeout = TimeSpan.FromSeconds(10),
            MaxRetries = 3
        };

        Action act = () =>
        {
            using var client = new OnePasswordClient(options);
        };

        // Assert
        act.Should().NotThrow("manual instantiation should still work for backward compatibility");
    }

    [Fact]
    public void OnePasswordClient_WithLogger_ShouldStillWork()
    {
        // Arrange & Act - Manual instantiation with logger (existing pattern)
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token"
        };

        var logger = NullLogger<OnePasswordClient>.Instance;

        Action act = () =>
        {
            using var client = new OnePasswordClient(options, logger);
        };

        // Assert
        act.Should().NotThrow("manual instantiation with logger should still work");
    }

    [Fact]
    public void OnePasswordClientOptions_WithDefaultValues_ShouldHaveExpectedDefaults()
    {
        // Arrange & Act
        var options = new OnePasswordClientOptions();

        // Assert - Verify new properties have correct defaults (FR-014, FR-015)
        options.MaxRetries.Should().Be(3, "default MaxRetries should be 3");
        options.Timeout.Should().Be(TimeSpan.FromSeconds(10), "default Timeout should be 10 seconds");
        options.RetryBaseDelay.Should().Be(TimeSpan.FromSeconds(1), "default RetryBaseDelay should be 1 second");
        options.RetryMaxDelay.Should().Be(TimeSpan.FromSeconds(30), "default RetryMaxDelay should be 30 seconds");
        options.EnableJitter.Should().BeTrue("default EnableJitter should be true");
        options.CircuitBreakerFailureThreshold.Should().Be(5, "default CircuitBreakerFailureThreshold should be 5");
        options.CircuitBreakerBreakDuration.Should().Be(TimeSpan.FromSeconds(30), "default CircuitBreakerBreakDuration should be 30 seconds");
        options.CircuitBreakerSamplingDuration.Should().Be(TimeSpan.FromSeconds(60), "default CircuitBreakerSamplingDuration should be 60 seconds");
    }

    [Fact]
    public void OnePasswordClientOptions_Validate_WithHttpUrl_ShouldThrow()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "http://localhost:8080", // HTTP not HTTPS
            Token = "test-token"
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must use HTTPS*", "HTTP URLs should be rejected for security (FR-037)");
    }

    [Fact]
    public void OnePasswordClientOptions_Validate_WithEmptyToken_ShouldThrow()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "" // Empty token
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Token is required*", "empty token should be rejected");
    }

    [Fact]
    public void OnePasswordClient_Dispose_ShouldNotThrowWhenCalledMultipleTimes()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token"
        };

        var client = new OnePasswordClient(options);

        // Act & Assert - Multiple dispose calls should be safe (idempotent)
        Action act = () =>
        {
            client.Dispose();
            client.Dispose();
            client.Dispose();
        };

        act.Should().NotThrow("Dispose should be idempotent");
    }

    [Fact]
    public void OnePasswordClient_AfterDispose_ShouldRejectNewRequests()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token"
        };

        var client = new OnePasswordClient(options);
        client.Dispose();

        // Act
        Func<Task> act = async () => await client.ListVaultsAsync();

        // Assert - FR-011: Reject new requests after disposal
        act.Should().ThrowAsync<ObjectDisposedException>(
            "requests after disposal should throw ObjectDisposedException");
    }
}
