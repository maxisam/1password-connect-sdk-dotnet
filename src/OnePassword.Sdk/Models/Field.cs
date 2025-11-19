// Domain Model: Field
// Feature: 001-onepassword-sdk

using System.Text.Json.Serialization;

namespace OnePassword.Sdk.Models;

/// <summary>
/// Represents a specific field within an item that holds a secret value or metadata.
/// </summary>
/// <remarks>
/// Field values containing secrets MUST NOT be logged or persisted in plaintext (FR-031, FR-035).
/// The ToString() method excludes the Value property to prevent accidental logging.
/// </remarks>
public class Field
{
    /// <summary>
    /// Gets the unique field identifier (UUID).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets the field name/label (e.g., "password", "username").
    /// </summary>
    [JsonPropertyName("label")]
    public string? Label { get; init; }

    /// <summary>
    /// Gets the field value (secret or non-secret).
    /// </summary>
    /// <remarks>
    /// This property contains sensitive data and MUST NOT be logged.
    /// Maximum size: 1MB (FR-023).
    /// </remarks>
    [JsonPropertyName("value")]
    public string? Value { get; init; }

    /// <summary>
    /// Gets the field type (STRING, PASSWORD, URL, etc.).
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FieldType Type { get; init; }

    /// <summary>
    /// Gets the field purpose (USERNAME, PASSWORD, etc.).
    /// </summary>
    [JsonPropertyName("purpose")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FieldPurpose Purpose { get; init; }

    /// <summary>
    /// Gets the optional section ID this field belongs to.
    /// </summary>
    [JsonPropertyName("section")]
    public string? SectionId { get; init; }

    /// <summary>
    /// Returns a string representation of the field, excluding the sensitive Value property.
    /// </summary>
    /// <returns>A string containing field metadata without the secret value.</returns>
    /// <remarks>
    /// Security: This override prevents accidental logging of secret values (FR-038, FR-043).
    /// </remarks>
    public override string ToString() => $"Field(Id={Id}, Label={Label}, Type={Type})";
}

/// <summary>
/// Specifies the type of a field.
/// </summary>
public enum FieldType
{
    /// <summary>
    /// Plain text field.
    /// </summary>
    STRING,

    /// <summary>
    /// Concealed text field (password).
    /// </summary>
    CONCEALED,

    /// <summary>
    /// Email address field.
    /// </summary>
    EMAIL,

    /// <summary>
    /// URL field.
    /// </summary>
    URL,

    /// <summary>
    /// Date field.
    /// </summary>
    DATE,

    /// <summary>
    /// Month and year field.
    /// </summary>
    MONTH_YEAR,

    /// <summary>
    /// Phone number field.
    /// </summary>
    PHONE
}

/// <summary>
/// Specifies the purpose of a field.
/// </summary>
public enum FieldPurpose
{
    /// <summary>
    /// Generic field with no specific purpose.
    /// </summary>
    NONE,

    /// <summary>
    /// Username field.
    /// </summary>
    USERNAME,

    /// <summary>
    /// Password field.
    /// </summary>
    PASSWORD,

    /// <summary>
    /// Notes field.
    /// </summary>
    NOTES
}
