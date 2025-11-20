// Domain Model: Vault
// Feature: 001-onepassword-sdk

using System.Text.Json.Serialization;

namespace OnePassword.Sdk.Models;

/// <summary>
/// Represents a secure container in 1Password that holds multiple items.
/// </summary>
/// <remarks>
/// Vaults are immutable from the SDK perspective (read-only operations).
/// Users must have read permission to access a vault.
/// </remarks>
public class Vault
{
    /// <summary>
    /// Gets the unique vault identifier (UUID).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets the display name of the vault (user-defined).
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional description of the vault.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

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
