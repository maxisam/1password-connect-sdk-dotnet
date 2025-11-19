// API Contract: OnePasswordConfigurationSource
// Feature: 001-onepassword-sdk

using Microsoft.Extensions.Configuration;

namespace OnePassword.Configuration;

/// <summary>
/// Configuration source for integrating 1Password secrets into .NET configuration system.
/// </summary>
/// <remarks>
/// Corresponds to FR-010: SDK MUST provide a configuration provider that integrates
/// with Microsoft.Extensions.Configuration
///
/// This source creates a provider that:
/// 1. Scans all configuration keys for op:// URIs (FR-011, FR-012, FR-014)
/// 2. Resolves URIs to actual secrets during configuration building (FR-013, FR-015, FR-016)
/// 3. Caches resolved secrets in memory (FR-020)
/// 4. Respects configuration precedence (environment variables override secrets) (FR-022, FR-023, FR-024)
/// </remarks>
public class OnePasswordConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// 1Password Connect server URL.
    /// </summary>
    /// <remarks>
    /// Required. Must be HTTPS URL. Typically loaded from configuration:
    /// - appsettings.json: OnePassword:ConnectServer
    /// - Environment variable: OnePassword__ConnectServer
    /// </remarks>
    public string ConnectServer { get; set; } = string.Empty;

    /// <summary>
    /// 1Password access token for authentication.
    /// </summary>
    /// <remarks>
    /// Required. Must be kept secure (not logged or committed to source control).
    /// Typically loaded from configuration:
    /// - appsettings.json: OnePassword:Token
    /// - Environment variable: OnePassword__Token
    ///
    /// Security: This value is SENSITIVE. Do NOT log or expose in error messages (FR-036).
    /// </remarks>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Builds the configuration provider instance.
    /// </summary>
    /// <param name="builder">The configuration builder</param>
    /// <returns>Configuration provider instance</returns>
    /// <remarks>
    /// Called by Microsoft.Extensions.Configuration infrastructure during
    /// configuration building. Creates provider that will scan and resolve
    /// op:// URIs when Load() is called.
    ///
    /// The builder is passed to the provider so it can scan all previous
    /// configuration sources for op:// URIs while respecting precedence
    /// (FR-024: environment variables override secrets).
    /// </remarks>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new OnePasswordConfigurationProvider(this, builder);
    }
}
