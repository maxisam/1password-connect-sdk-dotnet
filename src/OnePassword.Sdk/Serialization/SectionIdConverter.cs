// JSON Converter: SectionIdConverter
// Feature: 001-onepassword-sdk

using System.Text.Json;
using System.Text.Json.Serialization;

namespace OnePassword.Sdk.Serialization;

/// <summary>
/// Custom JSON converter for the section field that can handle both string and object formats.
/// </summary>
/// <remarks>
/// The 1Password Connect API can return the section field in two formats:
/// 1. As a string (section ID): "section": "section123"
/// 2. As an object with id and label: "section": {"id": "section123", "label": "Section Name"}
///
/// This converter handles both formats and extracts the section ID.
/// </remarks>
public class SectionIdConverter : JsonConverter<string?>
{
    /// <summary>
    /// Reads and converts the JSON to a section ID string.
    /// </summary>
    /// <param name="reader">The reader</param>
    /// <param name="typeToConvert">The type to convert</param>
    /// <param name="options">Serializer options</param>
    /// <returns>The section ID string, or null if not present</returns>
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Handle null
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        // Handle string format: "section123"
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }

        // Handle object format: {"id": "section123", "label": "Section Name"}
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var jsonDoc = JsonDocument.ParseValue(ref reader);
            if (jsonDoc.RootElement.TryGetProperty("id", out var idElement))
            {
                return idElement.GetString();
            }
            return null;
        }

        throw new JsonException($"Unexpected token type for section field: {reader.TokenType}");
    }

    /// <summary>
    /// Writes a section ID string to JSON.
    /// </summary>
    /// <param name="writer">The writer</param>
    /// <param name="value">The section ID value to write</param>
    /// <param name="options">Serializer options</param>
    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value);
        }
    }
}
