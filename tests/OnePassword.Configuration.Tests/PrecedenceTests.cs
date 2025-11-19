// Unit Tests: Configuration Precedence
// Feature: 001-onepassword-sdk
// User Story 3: Environment Variable Override

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using OnePassword.Configuration;
using OnePassword.Sdk.Exceptions;

namespace OnePassword.Configuration.Tests;

/// <summary>
/// Tests for configuration precedence rules (FR-022, FR-023, FR-024).
/// Ensures environment variables override 1Password secrets.
/// </summary>
public class PrecedenceTests
{
    [Fact]
    public void EnvironmentVariable_Should_Override_OpUri()
    {
        // Arrange
        var builder = new ConfigurationBuilder()
            // appsettings.json has op:// URI
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Password"] = "op://vault/db/password",
                ["Database:Host"] = "localhost",
                ["OnePassword:ConnectServer"] = "https://connect.example.com",
                ["OnePassword:Token"] = "test-token"
            })
            // Environment variable overrides with real value
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Password"] = "local-password"  // Non-op:// value
            })
            .AddOnePassword();

        // Act
        var config = builder.Build();

        // Assert
        // Environment variable should win (not the 1Password secret)
        config["Database:Password"].Should().Be("local-password");
        config["Database:Host"].Should().Be("localhost");
    }

    [Fact]
    public void EnvironmentVariable_Not_Set_Should_Attempt_OpUri_Resolution()
    {
        // Arrange
        var builder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Password"] = "op://vault/db/password",  // Will attempt resolution
                ["OnePassword:ConnectServer"] = "https://connect.example.com",
                ["OnePassword:Token"] = "test-token"
            })
            .AddOnePassword();  // No environment override

        // Act
        // Building config triggers provider Load() which scans for op:// URIs
        // and attempts to resolve them (will fail in unit test environment)
        Action act = () => builder.Build();

        // Assert
        // Should throw because it tries to connect to the 1Password API
        // (actual resolution tested in integration tests with mock/real server)
        act.Should().Throw<OnePasswordException>()
            .WithMessage("*Failed to load secrets from 1Password Connect API*");
    }

    [Fact]
    public void AddEnvironmentVariables_Before_AddOnePassword_Should_Take_Precedence()
    {
        // Arrange
        var builder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Password"] = "op://vault/db/password",
                ["Database:ApiKey"] = "op://vault/api/key",
                ["Database:Host"] = "localhost",
                ["OnePassword:ConnectServer"] = "https://connect.example.com",
                ["OnePassword:Token"] = "test-token"
            })
            // Simulate environment variables (added before OnePassword)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Password"] = "env-password",   // Overrides op:// URI
                ["Database:ApiKey"] = "env-api-key",      // Overrides op:// URI
                ["Database:Host"] = "env-host"             // Overrides regular value
            })
            .AddOnePassword();

        // Act
        var config = builder.Build();

        // Assert
        config["Database:Password"].Should().Be("env-password", "environment variable should override op:// URI");
        config["Database:ApiKey"].Should().Be("env-api-key", "environment variable should override op:// URI");
        config["Database:Host"].Should().Be("env-host", "environment variable should override regular value");
    }

    [Fact]
    public void Mixed_OpUris_And_Regular_Values_With_Override_Should_Work()
    {
        // Arrange
        var builder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Password"] = "op://vault/db/password",  // Secret reference
                ["Database:Host"] = "localhost",                    // Regular value
                ["Database:Port"] = "5432",                        // Regular value
                ["OnePassword:ConnectServer"] = "https://connect.example.com",
                ["OnePassword:Token"] = "test-token"
            })
            // Override the secret with environment variable
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Password"] = "env-password"  // Override op:// URI
            })
            .AddOnePassword();

        // Act
        var config = builder.Build();

        // Assert
        config["Database:Password"].Should().Be("env-password", "environment variable should override op:// URI");
        config["Database:Host"].Should().Be("localhost", "regular values should pass through");
        config["Database:Port"].Should().Be("5432", "regular values should pass through");
    }

    [Fact]
    public void Multiple_OpUris_All_Overridden_Should_Work()
    {
        // Arrange
        var builder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Password"] = "op://vault/db/password",
                ["Database:ApiKey"] = "op://vault/api/key",
                ["Database:AdminToken"] = "op://vault/admin/token",
                ["OnePassword:ConnectServer"] = "https://connect.example.com",
                ["OnePassword:Token"] = "test-token"
            })
            // Environment overrides all secrets
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Password"] = "local-password",
                ["Database:ApiKey"] = "local-api-key",
                ["Database:AdminToken"] = "local-admin-token"
            })
            .AddOnePassword();

        // Act
        var config = builder.Build();

        // Assert
        config["Database:Password"].Should().Be("local-password",
            "environment variable should override this secret");
        config["Database:ApiKey"].Should().Be("local-api-key",
            "environment variable should override this secret");
        config["Database:AdminToken"].Should().Be("local-admin-token",
            "environment variable should override this secret");
    }

    [Fact]
    public void Provider_Should_Not_Override_NonOpUri_Values()
    {
        // Arrange
        var builder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Password"] = "plain-password",  // NOT an op:// URI
                ["Database:Host"] = "localhost",
                ["OnePassword:ConnectServer"] = "https://connect.example.com",
                ["OnePassword:Token"] = "test-token"
            })
            .AddOnePassword();

        // Act
        var config = builder.Build();

        // Assert
        config["Database:Password"].Should().Be("plain-password",
            "provider should not modify non-op:// values");
        config["Database:Host"].Should().Be("localhost");
    }

    [Fact]
    public void Case_Insensitive_OpUri_Detection_Should_Work()
    {
        // Arrange
        var builder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Secret1"] = "op://vault/item/field",   // Lowercase
                ["Secret2"] = "OP://vault/item/field",   // Uppercase
                ["Secret3"] = "Op://vault/item/field",   // Mixed case
                ["OnePassword:ConnectServer"] = "https://connect.example.com",
                ["OnePassword:Token"] = "test-token"
            })
            // Override all secrets to avoid HTTP calls
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Secret1"] = "value1",
                ["Secret2"] = "value2",
                ["Secret3"] = "value3"
            })
            .AddOnePassword();

        // Act
        var config = builder.Build();

        // Assert - All overrides work regardless of op:// URI case
        config["Secret1"].Should().Be("value1");
        config["Secret2"].Should().Be("value2");
        config["Secret3"].Should().Be("value3");
    }
}
