// Unit Tests: Resilience Policy Builder
// Feature: 002-httpclient-factory-polly

using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OnePassword.Sdk.Client;
using OnePassword.Sdk.Resilience;

namespace OnePassword.Sdk.Tests.Unit;

/// <summary>
/// Unit tests for ResiliencePolicyBuilder.
/// </summary>
/// <remarks>
/// Verifies FR-003, FR-004, FR-014, FR-015: Policy configuration from options.
/// </remarks>
public class ResiliencePolicyBuilderTests
{
    [Fact]
    public void BuildRetryStrategy_WithValidOptions_ShouldReturnConfiguredStrategy()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            MaxRetries = 5,
            RetryBaseDelay = TimeSpan.FromMilliseconds(200),
            RetryMaxDelay = TimeSpan.FromSeconds(10),
            EnableJitter = false,
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        // Act
        var strategy = ResiliencePolicyBuilder.BuildRetryStrategy(options);

        // Assert
        strategy.Should().NotBeNull("strategy should be created from options");
        strategy.MaxRetryAttempts.Should().Be(5, "MaxRetries should map to MaxRetryAttempts");
        strategy.Delay.Should().Be(TimeSpan.FromMilliseconds(200), "RetryBaseDelay should map to Delay");
        strategy.MaxDelay.Should().Be(TimeSpan.FromSeconds(10), "RetryMaxDelay should map to MaxDelay");
        strategy.UseJitter.Should().BeFalse("EnableJitter should map to UseJitter");
    }

    [Fact]
    public void BuildRetryStrategy_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => ResiliencePolicyBuilder.BuildRetryStrategy(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void BuildCircuitBreakerStrategy_WithValidOptions_ShouldReturnConfiguredStrategy()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            CircuitBreakerFailureThreshold = 3,
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(15),
            CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(30),
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        // Act
        var strategy = ResiliencePolicyBuilder.BuildCircuitBreakerStrategy(options);

        // Assert
        strategy.Should().NotBeNull("strategy should be created from options");
        strategy.MinimumThroughput.Should().Be(3, "CircuitBreakerFailureThreshold should map to MinimumThroughput");
        strategy.BreakDuration.Should().Be(TimeSpan.FromSeconds(15), "CircuitBreakerBreakDuration should map to BreakDuration");
        strategy.SamplingDuration.Should().Be(TimeSpan.FromSeconds(30), "CircuitBreakerSamplingDuration should map to SamplingDuration");
    }

    [Fact]
    public void BuildCircuitBreakerStrategy_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => ResiliencePolicyBuilder.BuildCircuitBreakerStrategy(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void BuildTimeoutStrategy_WithValidOptions_ShouldReturnConfiguredStrategy()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            Timeout = TimeSpan.FromSeconds(30),
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        // Act
        var strategy = ResiliencePolicyBuilder.BuildTimeoutStrategy(options);

        // Assert
        strategy.Should().NotBeNull("strategy should be created from options");
        strategy.Timeout.Should().Be(TimeSpan.FromSeconds(30), "Timeout should map to strategy Timeout");
    }

    [Fact]
    public void BuildTimeoutStrategy_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => ResiliencePolicyBuilder.BuildTimeoutStrategy(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void BuildResiliencePipeline_WithValidOptions_ShouldReturnPipeline()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            MaxRetries = 3,
            Timeout = TimeSpan.FromSeconds(10),
            CircuitBreakerFailureThreshold = 5,
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        // Act
        var pipeline = ResiliencePolicyBuilder.BuildResiliencePipeline(options);

        // Assert
        pipeline.Should().NotBeNull("complete resilience pipeline should be created");
    }

    [Fact]
    public void BuildResiliencePipeline_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => ResiliencePolicyBuilder.BuildResiliencePipeline(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void BuildResiliencePipeline_ShouldExecuteWithoutErrors()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            Logger = NullLogger<OnePasswordClient>.Instance
        };

        var pipeline = ResiliencePolicyBuilder.BuildResiliencePipeline(options);

        // Act - Execute a simple operation through the pipeline
        Func<Task<HttpResponseMessage>> act = async () =>
        {
            return await pipeline.ExecuteAsync(async ct =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}")
                };
                return response;
            }, CancellationToken.None);
        };

        // Assert
        act.Should().NotThrowAsync("pipeline should execute successfully for successful responses");
    }

    [Fact]
    public void BuildRetryStrategy_WithDefaultOptions_ShouldUseExpectedDefaults()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            Logger = NullLogger<OnePasswordClient>.Instance
            // All resilience properties use defaults
        };

        // Act
        var strategy = ResiliencePolicyBuilder.BuildRetryStrategy(options);

        // Assert - Verify defaults match OnePasswordClientOptions defaults
        strategy.MaxRetryAttempts.Should().Be(3, "default MaxRetries is 3");
        strategy.Delay.Should().Be(TimeSpan.FromSeconds(1), "default RetryBaseDelay is 1 second");
        strategy.MaxDelay.Should().Be(TimeSpan.FromSeconds(30), "default RetryMaxDelay is 30 seconds");
        strategy.UseJitter.Should().BeTrue("default EnableJitter is true");
    }

    [Fact]
    public void BuildCircuitBreakerStrategy_WithDefaultOptions_ShouldUseExpectedDefaults()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            Logger = NullLogger<OnePasswordClient>.Instance
            // All resilience properties use defaults
        };

        // Act
        var strategy = ResiliencePolicyBuilder.BuildCircuitBreakerStrategy(options);

        // Assert - Verify defaults match OnePasswordClientOptions defaults
        strategy.MinimumThroughput.Should().Be(5, "default CircuitBreakerFailureThreshold is 5");
        strategy.BreakDuration.Should().Be(TimeSpan.FromSeconds(30), "default CircuitBreakerBreakDuration is 30 seconds");
        strategy.SamplingDuration.Should().Be(TimeSpan.FromSeconds(60), "default CircuitBreakerSamplingDuration is 60 seconds");
    }
}
