// Unit Tests: Exception Hierarchy
// Feature: 001-onepassword-sdk

using FluentAssertions;
using OnePassword.Sdk.Exceptions;

namespace OnePassword.Sdk.Tests.Exceptions;

/// <summary>
/// Tests for the exception hierarchy.
/// Validates that all exceptions construct correctly with proper context and error messages.
/// </summary>
public class ExceptionTests
{
    [Fact]
    public void OnePasswordException_Should_Construct_With_Message()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new OnePasswordException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void OnePasswordException_Should_Construct_With_Message_And_InnerException()
    {
        // Arrange
        var message = "Test error message";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new OnePasswordException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void AuthenticationException_Should_Include_Context_In_Message()
    {
        // Arrange
        var message = "Authentication failed: invalid token";

        // Act
        var exception = new AuthenticationException(message);

        // Assert
        exception.Message.Should().Contain("Authentication failed");
        exception.Message.Should().Contain("invalid token");
    }

    [Fact]
    public void AccessDeniedException_Should_Include_VaultId_And_ItemId()
    {
        // Arrange
        var message = "Access denied";
        var vaultId = "vault-123";
        var itemId = "item-456";

        // Act
        var exception = new AccessDeniedException(message, vaultId, itemId);

        // Assert
        exception.Message.Should().Be(message);
        exception.VaultId.Should().Be(vaultId);
        exception.ItemId.Should().Be(itemId);
    }

    [Fact]
    public void VaultNotFoundException_Should_Format_Message_With_VaultId()
    {
        // Arrange
        var vaultId = "production";

        // Act
        var exception = new VaultNotFoundException(vaultId);

        // Assert
        exception.Message.Should().Be("Vault 'production' not found or not accessible");
        exception.VaultId.Should().Be(vaultId);
    }

    [Fact]
    public void ItemNotFoundException_Should_Format_Message_With_Context()
    {
        // Arrange
        var vaultId = "production";
        var itemId = "database-creds";

        // Act
        var exception = new ItemNotFoundException(vaultId, itemId);

        // Assert
        exception.Message.Should().Be("Item 'database-creds' not found in vault 'production'");
        exception.VaultId.Should().Be(vaultId);
        exception.ItemId.Should().Be(itemId);
    }

    [Fact]
    public void FieldNotFoundException_Should_Include_Full_Context()
    {
        // Arrange
        var vaultId = "production";
        var itemId = "database-creds";
        var fieldLabel = "password";

        // Act
        var exception = new FieldNotFoundException(vaultId, itemId, fieldLabel);

        // Assert
        exception.Message.Should().Be("Field 'password' not found in item 'database-creds' in vault 'production'");
        exception.VaultId.Should().Be(vaultId);
        exception.ItemId.Should().Be(itemId);
        exception.FieldLabel.Should().Be(fieldLabel);
    }

    [Fact]
    public void NetworkException_Should_Include_Retry_Count()
    {
        // Arrange
        var message = "Connection failed";
        var retryAttempts = 3;

        // Act
        var exception = new NetworkException(message, retryAttempts);

        // Assert
        exception.Message.Should().Contain("Connection failed");
        exception.Message.Should().Contain("after 3 retry attempts");
        exception.RetryAttempts.Should().Be(retryAttempts);
    }

    [Fact]
    public void MalformedUriException_Should_Include_Full_Context()
    {
        // Arrange
        var configKey = "Database:Password";
        var malformedUri = "op://vault/item/";
        var reason = "Empty field component";

        // Act
        var exception = new MalformedUriException(configKey, malformedUri, reason);

        // Assert
        exception.Message.Should().Contain("Database:Password");
        exception.Message.Should().Contain("Empty field component");
        exception.Message.Should().Contain("op://<vault>/<item>/<field>");
        exception.ConfigurationKey.Should().Be(configKey);
        exception.MalformedUri.Should().Be(malformedUri);
    }

    [Fact]
    public void BatchSizeExceededException_Should_Include_Counts()
    {
        // Arrange
        var requestedCount = 150;
        var maximumAllowed = 100;

        // Act
        var exception = new BatchSizeExceededException(requestedCount, maximumAllowed);

        // Assert
        exception.Message.Should().Contain("150 secrets requested");
        exception.Message.Should().Contain("maximum is 100");
        exception.RequestedCount.Should().Be(requestedCount);
        exception.MaximumAllowed.Should().Be(maximumAllowed);
    }

    [Fact]
    public void SecretSizeExceededException_Should_Include_Size_Information()
    {
        // Arrange
        var vaultId = "prod";
        var itemId = "large-secret";
        var fieldLabel = "certificate";
        var actualSizeBytes = 2097152L; // 2MB
        var maximumSizeBytes = 1048576L; // 1MB

        // Act
        var exception = new SecretSizeExceededException(vaultId, itemId, fieldLabel, actualSizeBytes, maximumSizeBytes);

        // Assert
        exception.Message.Should().Contain("2.00MB");
        exception.Message.Should().Contain("maximum is 1MB");
        exception.VaultId.Should().Be(vaultId);
        exception.ItemId.Should().Be(itemId);
        exception.FieldLabel.Should().Be(fieldLabel);
        exception.ActualSizeBytes.Should().Be(actualSizeBytes);
        exception.MaximumSizeBytes.Should().Be(maximumSizeBytes);
    }

    [Fact]
    public void BatchTimeoutException_Should_Include_Timeout_Duration()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(10);

        // Act
        var exception = new BatchTimeoutException(timeout);

        // Assert
        exception.Message.Should().Contain("timed out after 10 seconds");
        exception.Timeout.Should().Be(timeout);
    }

    [Fact]
    public void All_Exceptions_Should_Inherit_From_OnePasswordException()
    {
        // Arrange & Act & Assert
        new AuthenticationException("test").Should().BeAssignableTo<OnePasswordException>();
        new AccessDeniedException("test").Should().BeAssignableTo<OnePasswordException>();
        new VaultNotFoundException("test").Should().BeAssignableTo<OnePasswordException>();
        new ItemNotFoundException("v", "i").Should().BeAssignableTo<OnePasswordException>();
        new FieldNotFoundException("v", "i", "f").Should().BeAssignableTo<OnePasswordException>();
        new NetworkException("test").Should().BeAssignableTo<OnePasswordException>();
        new MalformedUriException("k", "u", "r").Should().BeAssignableTo<OnePasswordException>();
        new BatchSizeExceededException(1).Should().BeAssignableTo<OnePasswordException>();
        new SecretSizeExceededException("v", "i", "f", 1).Should().BeAssignableTo<OnePasswordException>();
        new BatchTimeoutException(TimeSpan.FromSeconds(1)).Should().BeAssignableTo<OnePasswordException>();
    }
}
