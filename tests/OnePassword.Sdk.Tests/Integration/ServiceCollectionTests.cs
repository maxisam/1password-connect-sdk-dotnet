// Integration Tests: Service Collection Registration
// Feature: 002-httpclient-factory-polly

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OnePassword.Sdk.Client;
using OnePassword.Sdk.Extensions;

namespace OnePassword.Sdk.Tests.Integration;

/// <summary>
/// Integration tests for DI service collection registration.
/// </summary>
/// <remarks>
/// Verifies FR-012: Dependency injection patterns for consumers using IServiceCollection.
/// </remarks>
public class ServiceCollectionTests
{
    [Fact]
    public void AddOnePasswordClient_WithActionConfiguration_ShouldRegisterClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        // Act
        services.AddOnePasswordClient(options =>
        {
            options.ConnectServer = "https://localhost:8080";
            options.Token = "test-token";
            options.MaxRetries = 5;
            options.CircuitBreakerFailureThreshold = 3;
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var client = serviceProvider.GetService<IOnePasswordClient>();
        client.Should().NotBeNull("AddOnePasswordClient should register IOnePasswordClient");
        client.Should().BeAssignableTo<IOnePasswordClient>("should implement IOnePasswordClient interface");
    }

    [Fact]
    public void AddOnePasswordClient_WithPreConfiguredOptions_ShouldRegisterClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://localhost:8080",
            Token = "test-token",
            MaxRetries = 3,
            Timeout = TimeSpan.FromSeconds(15)
        };

        // Act
        services.AddOnePasswordClient(options);

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var client = serviceProvider.GetService<IOnePasswordClient>();
        client.Should().NotBeNull("AddOnePasswordClient should register IOnePasswordClient with pre-configured options");
    }

    [Fact]
    public void AddOnePasswordClient_ShouldRegisterHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        // Act
        services.AddOnePasswordClient(options =>
        {
            options.ConnectServer = "https://localhost:8080";
            options.Token = "test-token";
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        httpClientFactory.Should().NotBeNull("AddOnePasswordClient should register IHttpClientFactory");

        var httpClient = httpClientFactory?.CreateClient("OnePasswordClient");
        httpClient.Should().NotBeNull("should create named HttpClient 'OnePasswordClient'");
        httpClient?.BaseAddress.Should().Be(new Uri("https://localhost:8080"));
    }

    [Fact]
    public void AddOnePasswordClient_ShouldConfigureResiliencePolicies()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        // Act
        services.AddOnePasswordClient(options =>
        {
            options.ConnectServer = "https://localhost:8080";
            options.Token = "test-token";
            options.MaxRetries = 5;
            options.RetryBaseDelay = TimeSpan.FromMilliseconds(200);
            options.CircuitBreakerFailureThreshold = 3;
            options.CircuitBreakerBreakDuration = TimeSpan.FromSeconds(10);
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var client = serviceProvider.GetService<IOnePasswordClient>();
        client.Should().NotBeNull("client should be registered with configured resilience policies");

        // Note: Actual policy behavior would be tested in integration tests with WireMock
        // This test verifies registration succeeds with custom policy configuration
    }

    [Fact]
    public void AddOnePasswordClient_WithInvalidOptions_ShouldThrowOnValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        services.AddOnePasswordClient(options =>
        {
            options.ConnectServer = "http://localhost:8080"; // HTTP instead of HTTPS - should fail validation
            options.Token = "test-token";
        });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        Action act = () => serviceProvider.GetRequiredService<IOnePasswordClient>();

        // Assert
        act.Should().Throw<ArgumentException>("invalid options should be caught during client creation")
            .WithMessage("*must use HTTPS*");
    }

    [Fact]
    public void AddOnePasswordClient_MultipleTimes_ShouldUseLast()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        // Act - Register twice with different configurations
        services.AddOnePasswordClient(options =>
        {
            options.ConnectServer = "https://first.example.com";
            options.Token = "first-token";
        });

        services.AddOnePasswordClient(options =>
        {
            options.ConnectServer = "https://second.example.com";
            options.Token = "second-token";
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        var httpClient = httpClientFactory?.CreateClient("OnePasswordClient");

        httpClient?.BaseAddress.Should().Be(new Uri("https://second.example.com"),
            "last registration should win");
    }
}
