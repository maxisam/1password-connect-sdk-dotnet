// Domain Model: Item
// Feature: 001-onepassword-sdk

using System.Text.Json.Serialization;

namespace OnePassword.Sdk.Models;

/// <summary>
/// Represents an individual entry within a vault that contains one or more secret fields.
/// </summary>
/// <remarks>
/// Items are immutable from the SDK perspective (read-only operations).
/// Each item must contain at least one field.
/// </remarks>
public class Item
{
    /// <summary>
    /// Gets the unique item identifier (UUID).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets the parent vault ID.
    /// </summary>
    [JsonPropertyName("vault")]
    public VaultReference Vault { get; init; } = new();

    /// <summary>
    /// Gets the display title (user-defined).
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Gets the item category (e.g., "LOGIN", "PASSWORD", "API_CREDENTIAL").
    /// </summary>
    [JsonPropertyName("category")]
    public string? Category { get; init; }

    /// <summary>
    /// Gets the collection of secret fields.
    /// </summary>
    [JsonPropertyName("fields")]
    public IReadOnlyList<Field> Fields { get; init; } = Array.Empty<Field>();

    /// <summary>
    /// Gets the optional sections grouping fields.
    /// </summary>
    [JsonPropertyName("sections")]
    public IReadOnlyList<Section>? Sections { get; init; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the last modification timestamp.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Represents a reference to a vault within an item.
/// </summary>
public class VaultReference
{
    /// <summary>
    /// Gets the vault ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;
}
