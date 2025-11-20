// Unit Tests: OnePasswordConfigurationProvider
// Feature: 001-onepassword-sdk

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using OnePassword.Configuration;
using OnePassword.Sdk.Exceptions;

namespace OnePassword.Configuration.Tests;

/// <summary>
/// Unit tests for OnePasswordConfigurationProvider and ConfigurationBuilderExtensions.
/// </summary>
public class ConfigurationProviderTests
{
    [Fact]
    public void AddOnePassword_With_Action_Should_Add_Source()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        builder.AddOnePassword(options =>
        {
            options.ConnectServer = "https://connect.example.com";
            options.Token = "test-token";
        });

        // Assert
        var sources = builder.Sources;
        sources.Should().HaveCount(1);
        sources[0].Should().BeOfType<OnePasswordConfigurationSource>();
    }

    [Fact]
    public void AddOnePassword_With_Explicit_Credentials_Should_Add_Source()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        builder.AddOnePassword("https://connect.example.com", "test-token");

        // Assert
        var sources = builder.Sources;
        sources.Should().HaveCount(1);
        sources[0].Should().BeOfType<OnePasswordConfigurationSource>();
    }

    [Fact]
    public void AddOnePassword_Should_Throw_When_Builder_Null()
    {
        // Arrange
        IConfigurationBuilder? builder = null;

        // Act
        Action act = () => builder!.AddOnePassword("https://connect.example.com", "token");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddOnePassword_Should_Throw_When_ConnectServer_Null()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        Action act = () => builder.AddOnePassword(null!, "token");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddOnePassword_Should_Throw_When_Token_Null()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        Action act = () => builder.AddOnePassword("https://connect.example.com", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddOnePassword_Should_Throw_When_ConfigureOptions_Null()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        Action act = () => builder.AddOnePassword((Action<OnePasswordConfigurationSource>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddOnePassword_Should_Return_Builder_For_Chaining()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        var result = builder.AddOnePassword(options =>
        {
            options.ConnectServer = "https://connect.example.com";
            options.Token = "test-token";
        });

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void AddOnePassword_From_Configuration_Should_Read_Settings()
    {
        // Arrange
        var builder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OnePassword:ConnectServer"] = "https://connect.example.com",
                ["OnePassword:Token"] = "test-token-from-config"
            });

        // Act
        builder.AddOnePassword();

        // Assert
        var sources = builder.Sources;
        sources.Should().HaveCount(2); // InMemory + OnePassword
        sources[1].Should().BeOfType<OnePasswordConfigurationSource>();

        var opSource = sources[1] as OnePasswordConfigurationSource;
        opSource!.ConnectServer.Should().Be("https://connect.example.com");
        opSource.Token.Should().Be("test-token-from-config");
    }

    [Fact]
    public void AddOnePassword_From_Configuration_Should_Prefer_Environment_Variables()
    {
        // Arrange
        var builder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OnePassword:ConnectServer"] = "https://config-server.com",
                ["OnePassword:Token"] = "config-token"
            })
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OnePassword:ConnectServer"] = "https://env-server.com",  // Environment override
                ["OnePassword:Token"] = "env-token"
            });

        // Act
        builder.AddOnePassword();

        // Assert
        var opSource = builder.Sources.OfType<OnePasswordConfigurationSource>().First();
        opSource.ConnectServer.Should().Be("https://env-server.com"); // Environment wins
        opSource.Token.Should().Be("env-token");
    }

    [Fact]
    public void ConfigurationSource_Build_Should_Return_Provider()
    {
        // Arrange
        var source = new OnePasswordConfigurationSource
        {
            ConnectServer = "https://connect.example.com",
            Token = "test-token"
        };

        var builder = new ConfigurationBuilder();

        // Act
        var provider = source.Build(builder);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeAssignableTo<IConfigurationProvider>();
    }

    [Fact]
    public void Provider_Load_Should_Throw_When_ConnectServer_Missing()
    {
        // Arrange
        var source = new OnePasswordConfigurationSource
        {
            ConnectServer = "", // Missing
            Token = "test-token"
        };

        var provider = source.Build(new ConfigurationBuilder());

        // Act
        Action act = () => provider.Load();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OnePassword:ConnectServer*");
    }

    [Fact]
    public void Provider_Load_Should_Throw_When_Token_Missing()
    {
        // Arrange
        var source = new OnePasswordConfigurationSource
        {
            ConnectServer = "https://connect.example.com",
            Token = "" // Missing
        };

        var provider = source.Build(new ConfigurationBuilder());

        // Act
        Action act = () => provider.Load();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OnePassword:Token*");
    }

    [Fact]
    public void Provider_Should_Not_Fail_When_No_OpUris_Present()
    {
        // Arrange
        var builder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Host"] = "localhost",
                ["Database:Port"] = "5432",
                ["OnePassword:ConnectServer"] = "https://connect.example.com",
                ["OnePassword:Token"] = "test-token"
            })
            .AddOnePassword();

        // Act
        Action act = () => builder.Build();

        // Assert - Should not throw, just do nothing
        act.Should().NotThrow();
    }

    [Fact]
    public void Multiple_AddOnePassword_Calls_Should_Add_Multiple_Sources()
    {
        // Arrange
        var builder = new ConfigurationBuilder()
            .AddOnePassword("https://server1.com", "token1")
            .AddOnePassword("https://server2.com", "token2");

        // Act
        var sources = builder.Sources.OfType<OnePasswordConfigurationSource>().ToList();

        // Assert
        sources.Should().HaveCount(2);
        sources[0].ConnectServer.Should().Be("https://server1.com");
        sources[1].ConnectServer.Should().Be("https://server2.com");
    }
}
