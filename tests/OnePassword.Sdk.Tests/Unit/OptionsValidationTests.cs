// Unit Tests: Options Validation
// Feature: 002-httpclient-factory-polly

using FluentAssertions;
using OnePassword.Sdk.Client;

namespace OnePassword.Sdk.Tests.Unit;

/// <summary>
/// Unit tests for OnePasswordClientOptions validation.
/// </summary>
/// <remarks>
/// Verifies FR-018: Configuration validation at client initialization.
/// </remarks>
public class OptionsValidationTests
{
    [Fact]
    public void Validate_WithValidOptions_ShouldNotThrow()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            Timeout = TimeSpan.FromSeconds(10),
            MaxRetries = 3,
            RetryBaseDelay = TimeSpan.FromSeconds(1),
            RetryMaxDelay = TimeSpan.FromSeconds(30),
            CircuitBreakerFailureThreshold = 5,
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(30),
            CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(60)
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().NotThrow("all options are valid");
    }

    [Fact]
    public void Validate_WithEmptyConnectServer_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "",
            Token = "test-token"
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ConnectServer is required*")
            .WithParameterName("ConnectServer");
    }

    [Fact]
    public void Validate_WithHttpConnectServer_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "http://localhost:8080",
            Token = "test-token"
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must use HTTPS*")
            .WithParameterName("ConnectServer");
    }

    [Fact]
    public void Validate_WithEmptyToken_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = ""
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Token is required*")
            .WithParameterName("Token");
    }

    [Fact]
    public void Validate_WithZeroTimeout_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            Timeout = TimeSpan.Zero
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Timeout must be greater than zero*")
            .WithParameterName("Timeout");
    }

    [Fact]
    public void Validate_WithNegativeTimeout_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            Timeout = TimeSpan.FromSeconds(-1)
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Timeout must be greater than zero*")
            .WithParameterName("Timeout");
    }

    [Fact]
    public void Validate_WithNegativeMaxRetries_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            MaxRetries = -1
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*MaxRetries must be greater than or equal to zero*")
            .WithParameterName("MaxRetries");
    }

    [Fact]
    public void Validate_WithZeroRetryBaseDelay_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            RetryBaseDelay = TimeSpan.Zero
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*RetryBaseDelay must be greater than zero*")
            .WithParameterName("RetryBaseDelay");
    }

    [Fact]
    public void Validate_WithNegativeRetryBaseDelay_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            RetryBaseDelay = TimeSpan.FromSeconds(-1)
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*RetryBaseDelay must be greater than zero*")
            .WithParameterName("RetryBaseDelay");
    }

    [Fact]
    public void Validate_WithRetryMaxDelayLessThanBaseDelay_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            RetryBaseDelay = TimeSpan.FromSeconds(10),
            RetryMaxDelay = TimeSpan.FromSeconds(5)
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*RetryMaxDelay must be greater than or equal to RetryBaseDelay*")
            .WithParameterName("RetryMaxDelay");
    }

    [Fact]
    public void Validate_WithZeroCircuitBreakerFailureThreshold_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            CircuitBreakerFailureThreshold = 0
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*CircuitBreakerFailureThreshold must be at least 1*")
            .WithParameterName("CircuitBreakerFailureThreshold");
    }

    [Fact]
    public void Validate_WithNegativeCircuitBreakerFailureThreshold_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            CircuitBreakerFailureThreshold = -1
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*CircuitBreakerFailureThreshold must be at least 1*")
            .WithParameterName("CircuitBreakerFailureThreshold");
    }

    [Fact]
    public void Validate_WithZeroCircuitBreakerBreakDuration_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            CircuitBreakerBreakDuration = TimeSpan.Zero
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*CircuitBreakerBreakDuration must be greater than zero*")
            .WithParameterName("CircuitBreakerBreakDuration");
    }

    [Fact]
    public void Validate_WithNegativeCircuitBreakerBreakDuration_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(-1)
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*CircuitBreakerBreakDuration must be greater than zero*")
            .WithParameterName("CircuitBreakerBreakDuration");
    }

    [Fact]
    public void Validate_WithZeroCircuitBreakerSamplingDuration_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            CircuitBreakerSamplingDuration = TimeSpan.Zero
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*CircuitBreakerSamplingDuration must be greater than zero*")
            .WithParameterName("CircuitBreakerSamplingDuration");
    }

    [Fact]
    public void Validate_WithNegativeCircuitBreakerSamplingDuration_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(-1)
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*CircuitBreakerSamplingDuration must be greater than zero*")
            .WithParameterName("CircuitBreakerSamplingDuration");
    }
}
