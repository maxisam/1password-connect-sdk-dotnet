// API Contract: Exception Hierarchy
// Feature: 001-onepassword-sdk
// Purpose: Base exception for all 1Password SDK errors

namespace OnePassword.Sdk.Exceptions;

/// <summary>
/// Base exception for all 1Password SDK errors.
/// </summary>
/// <remarks>
/// Corresponds to FR-025: SDK MUST throw specific exceptions for different error types
///
/// All SDK exceptions inherit from this base class, allowing consumers to catch
/// all 1Password errors with a single catch block if desired.
///
/// Exception hierarchy follows Constitution Principle III (API Simplicity) by providing
/// clear, specific exception types with actionable error messages (FR-026, FR-029, FR-032).
/// </remarks>
public class OnePasswordException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OnePasswordException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public OnePasswordException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="OnePasswordException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public OnePasswordException(string message, Exception innerException) : base(message, innerException) { }
}
