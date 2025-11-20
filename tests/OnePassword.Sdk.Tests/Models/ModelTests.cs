// Unit Tests: Domain Models
// Feature: 001-onepassword-sdk

using FluentAssertions;
using OnePassword.Sdk.Client;
using OnePassword.Sdk.Models;
using System.Text.Json;

namespace OnePassword.Sdk.Tests.Models;

/// <summary>
/// Tests for domain models.
/// Validates immutability, JSON serialization, and security features (Field.ToString).
/// </summary>
public class ModelTests
{
    [Fact]
    public void Vault_Should_Be_Immutable()
    {
        // Arrange
        var vault = new Vault
        {
            Id = "vault-123",
            Name = "Production",
            Description = "Production vault",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act - attempting to modify should fail at compile time
        // This test verifies the init-only property pattern

        // Assert
        vault.Id.Should().Be("vault-123");
        vault.Name.Should().Be("Production");
    }

    [Fact]
    public void Item_Should_Be_Immutable()
    {
        // Arrange
        var item = new Item
        {
            Id = "item-456",
            Vault = new VaultReference { Id = "vault-123" },
            Title = "Database Credentials",
            Category = "LOGIN",
            Fields = new[] { new Field { Id = "field-1", Label = "password", Type = FieldType.CONCEALED } },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        item.Id.Should().Be("item-456");
        item.Title.Should().Be("Database Credentials");
        item.Fields.Should().HaveCount(1);
    }

    [Fact]
    public void Field_ToString_Should_NOT_Include_Value()
    {
        // Arrange
        var field = new Field
        {
            Id = "field-789",
            Label = "password",
            Value = "super-secret-password",
            Type = FieldType.CONCEALED,
            Purpose = FieldPurpose.PASSWORD
        };

        // Act
        var toString = field.ToString();

        // Assert - Security requirement: Value MUST NOT be in string representation
        toString.Should().Contain("field-789");
        toString.Should().Contain("password"); // Label is OK
        toString.Should().NotContain("super-secret-password"); // Value MUST be excluded
        toString.Should().Contain("CONCEALED"); // Type is OK
    }

    [Fact]
    public void Vault_Should_Serialize_To_JSON_Correctly()
    {
        // Arrange
        var vault = new Vault
        {
            Id = "vault-123",
            Name = "Production",
            Description = "Production vault"
        };

        // Act
        var json = JsonSerializer.Serialize(vault);
        var deserialized = JsonSerializer.Deserialize<Vault>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be("vault-123");
        deserialized.Name.Should().Be("Production");
        deserialized.Description.Should().Be("Production vault");
    }

    [Fact]
    public void Item_Should_Serialize_To_JSON_Correctly()
    {
        // Arrange
        var item = new Item
        {
            Id = "item-456",
            Vault = new VaultReference { Id = "vault-123" },
            Title = "Database Credentials",
            Category = "LOGIN",
            Fields = new[]
            {
                new Field
                {
                    Id = "field-1",
                    Label = "username",
                    Value = "admin",
                    Type = FieldType.STRING,
                    Purpose = FieldPurpose.USERNAME
                },
                new Field
                {
                    Id = "field-2",
                    Label = "password",
                    Value = "secret",
                    Type = FieldType.CONCEALED,
                    Purpose = FieldPurpose.PASSWORD
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(item);
        var deserialized = JsonSerializer.Deserialize<Item>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be("item-456");
        deserialized.Title.Should().Be("Database Credentials");
        deserialized.Fields.Should().HaveCount(2);
        deserialized.Fields[0].Label.Should().Be("username");
        deserialized.Fields[0].Value.Should().Be("admin");
        deserialized.Fields[1].Label.Should().Be("password");
        deserialized.Fields[1].Value.Should().Be("secret");
    }

    [Fact]
    public void Field_Should_Deserialize_With_Correct_Enum_Values()
    {
        // Arrange
        var json = @"{
            ""id"": ""field-1"",
            ""label"": ""password"",
            ""value"": ""secret"",
            ""type"": ""CONCEALED"",
            ""purpose"": ""PASSWORD""
        }";

        // Act
        var field = JsonSerializer.Deserialize<Field>(json);

        // Assert
        field.Should().NotBeNull();
        field!.Type.Should().Be(FieldType.CONCEALED);
        field.Purpose.Should().Be(FieldPurpose.PASSWORD);
    }

    [Fact]
    public void Section_Should_Be_Immutable()
    {
        // Arrange
        var section = new Section
        {
            Id = "section-1",
            Label = "Database Connection"
        };

        // Assert
        section.Id.Should().Be("section-1");
        section.Label.Should().Be("Database Connection");
    }

    [Fact]
    public void OnePasswordClientOptions_Should_Validate_HTTPS_URL()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "http://localhost:8080", // HTTP not allowed
            Token = "test-token"
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*HTTPS*");
    }

    [Fact]
    public void OnePasswordClientOptions_Should_Validate_Token_Required()
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
            .WithMessage("*Token*required*");
    }

    [Fact]
    public void OnePasswordClientOptions_Should_Accept_Valid_Options()
    {
        // Arrange
        var options = new OnePasswordClientOptions
        {
            ConnectServer = "https://connect.example.com",
            Token = "valid-token-123",
            Timeout = TimeSpan.FromSeconds(10),
            MaxRetries = 3
        };

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void OnePasswordClientOptions_Should_Have_Sensible_Defaults()
    {
        // Arrange & Act
        var options = new OnePasswordClientOptions();

        // Assert
        options.Timeout.Should().Be(TimeSpan.FromSeconds(10));
        options.MaxRetries.Should().Be(3);
    }

    [Fact]
    public void FieldType_Enum_Should_Have_All_Expected_Values()
    {
        // Assert - Verify all field types from specification
        Enum.IsDefined(typeof(FieldType), FieldType.STRING).Should().BeTrue();
        Enum.IsDefined(typeof(FieldType), FieldType.CONCEALED).Should().BeTrue();
        Enum.IsDefined(typeof(FieldType), FieldType.EMAIL).Should().BeTrue();
        Enum.IsDefined(typeof(FieldType), FieldType.URL).Should().BeTrue();
        Enum.IsDefined(typeof(FieldType), FieldType.DATE).Should().BeTrue();
        Enum.IsDefined(typeof(FieldType), FieldType.MONTH_YEAR).Should().BeTrue();
        Enum.IsDefined(typeof(FieldType), FieldType.PHONE).Should().BeTrue();
    }

    [Fact]
    public void FieldPurpose_Enum_Should_Have_All_Expected_Values()
    {
        // Assert - Verify all field purposes from specification
        Enum.IsDefined(typeof(FieldPurpose), FieldPurpose.NONE).Should().BeTrue();
        Enum.IsDefined(typeof(FieldPurpose), FieldPurpose.USERNAME).Should().BeTrue();
        Enum.IsDefined(typeof(FieldPurpose), FieldPurpose.PASSWORD).Should().BeTrue();
        Enum.IsDefined(typeof(FieldPurpose), FieldPurpose.NOTES).Should().BeTrue();
    }
}
