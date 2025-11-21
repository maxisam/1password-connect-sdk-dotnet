// Unit Tests: Transient Error Detector
// Feature: 002-httpclient-factory-polly

using System.Net;
using FluentAssertions;
using OnePassword.Sdk.Resilience;

namespace OnePassword.Sdk.Tests.Unit;

/// <summary>
/// Unit tests for TransientErrorDetector class.
/// </summary>
/// <remarks>
/// Verifies FR-005 and FR-010: Correct classification of transient vs permanent errors.
/// </remarks>
public class TransientErrorDetectorTests
{
    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]          // 408
    [InlineData(HttpStatusCode.TooManyRequests)]         // 429
    [InlineData(HttpStatusCode.InternalServerError)]     // 500
    [InlineData(HttpStatusCode.BadGateway)]              // 502
    [InlineData(HttpStatusCode.ServiceUnavailable)]      // 503
    [InlineData(HttpStatusCode.GatewayTimeout)]          // 504
    public void IsTransient_ShouldReturnTrue_ForTransientStatusCodes(HttpStatusCode statusCode)
    {
        // Act
        var result = TransientErrorDetector.IsTransient(statusCode);

        // Assert
        result.Should().BeTrue($"status code {(int)statusCode} ({statusCode}) should be classified as transient");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]            // 401
    [InlineData(HttpStatusCode.Forbidden)]               // 403
    [InlineData(HttpStatusCode.NotFound)]                // 404
    public void IsPermanent_ShouldReturnTrue_ForPermanentStatusCodes(HttpStatusCode statusCode)
    {
        // Act
        var result = TransientErrorDetector.IsPermanent(statusCode);

        // Assert
        result.Should().BeTrue($"status code {(int)statusCode} ({statusCode}) should be classified as permanent (non-retryable)");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]            // 401
    [InlineData(HttpStatusCode.Forbidden)]               // 403
    [InlineData(HttpStatusCode.NotFound)]                // 404
    public void IsTransient_ShouldReturnFalse_ForPermanentStatusCodes(HttpStatusCode statusCode)
    {
        // Act
        var result = TransientErrorDetector.IsTransient(statusCode);

        // Assert
        result.Should().BeFalse($"status code {(int)statusCode} ({statusCode}) should NOT be classified as transient");
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]          // 408
    [InlineData(HttpStatusCode.TooManyRequests)]         // 429
    [InlineData(HttpStatusCode.InternalServerError)]     // 500
    [InlineData(HttpStatusCode.BadGateway)]              // 502
    [InlineData(HttpStatusCode.ServiceUnavailable)]      // 503
    [InlineData(HttpStatusCode.GatewayTimeout)]          // 504
    public void IsPermanent_ShouldReturnFalse_ForTransientStatusCodes(HttpStatusCode statusCode)
    {
        // Act
        var result = TransientErrorDetector.IsPermanent(statusCode);

        // Assert
        result.Should().BeFalse($"status code {(int)statusCode} ({statusCode}) should NOT be classified as permanent");
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]                      // 200
    [InlineData(HttpStatusCode.Created)]                 // 201
    [InlineData(HttpStatusCode.BadRequest)]              // 400
    public void IsTransient_ShouldReturnFalse_ForNonTransientStatusCodes(HttpStatusCode statusCode)
    {
        // Act
        var result = TransientErrorDetector.IsTransient(statusCode);

        // Assert
        result.Should().BeFalse($"status code {(int)statusCode} ({statusCode}) is neither transient nor explicitly permanent");
    }

    [Fact]
    public void IsTransient_WithHttpResponseMessage_ShouldReturnTrue_ForTransientError()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

        // Act
        var result = TransientErrorDetector.IsTransient(response);

        // Assert
        result.Should().BeTrue("503 Service Unavailable should be classified as transient");
    }

    [Fact]
    public void IsPermanent_WithHttpResponseMessage_ShouldReturnTrue_ForPermanentError()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);

        // Act
        var result = TransientErrorDetector.IsPermanent(response);

        // Assert
        result.Should().BeTrue("401 Unauthorized should be classified as permanent");
    }

    [Fact]
    public void IsTransient_WithNullHttpResponseMessage_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => TransientErrorDetector.IsTransient((HttpResponseMessage)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("response");
    }

    [Fact]
    public void IsPermanent_WithNullHttpResponseMessage_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => TransientErrorDetector.IsPermanent((HttpResponseMessage)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("response");
    }
}
