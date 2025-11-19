// Domain Model: Section
// Feature: 001-onepassword-sdk

using System.Text.Json.Serialization;

namespace OnePassword.Sdk.Models;

/// <summary>
/// Represents a logical grouping of fields within an item.
/// </summary>
public class Section
{
    /// <summary>
    /// Gets the unique section identifier (UUID).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets the section name (user-defined).
    /// </summary>
    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;
}
