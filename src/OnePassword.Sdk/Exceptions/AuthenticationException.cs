// API Contract: Authentication Exception
// Feature: 001-onepassword-sdk

namespace OnePassword.Sdk.Exceptions;

/// <summary>
/// Thrown when authentication fails due to invalid or expired token.
/// </summary>
/// <remarks>
/// Corresponds to FR-025: authentication failure exception type
/// Corresponds to FR-021: token expiration error
/// Corresponds to FR-008, FR-009: authentication error handling
///
/// Typical causes:
/// - Invalid access token
/// - Expired token (for long-running applications)
/// - Token revoked by administrator
///
/// Resolution: Verify token is correct, check token expiration, restart application
/// with valid token.
///
/// Security: Error message MUST NOT include the token value (FR-036).
/// </remarks>
public class AuthenticationException : OnePasswordException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the authentication error.</param>
    public AuthenticationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public AuthenticationException(string message, Exception innerException) : base(message, innerException) { }
}
