// API Contract: IOnePasswordConfigurationSource
// Feature: 001-onepassword-sdk
// Purpose: Configuration source integration for Microsoft.Extensions.Configuration

using Microsoft.Extensions.Configuration;

namespace OnePassword.Configuration
{
    /// <summary>
    /// Configuration source for integrating 1Password secrets into .NET configuration system.
    /// </summary>
    /// <remarks>
    /// Corresponds to FR-010: SDK MUST provide a configuration provider that integrates
    /// with Microsoft.Extensions.Configuration
    ///
    /// This source creates an <see cref="IOnePasswordConfigurationProvider"/> that:
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
        public string ConnectServer { get; set; }

        /// <summary>
        /// 1Password service account token for authentication.
        /// </summary>
        /// <remarks>
        /// Required. Must be kept secure (not logged or committed to source control).
        /// Typically loaded from configuration:
        /// - appsettings.json: OnePassword:Token
        /// - Environment variable: OnePassword__Token
        ///
        /// Security: This value is SENSITIVE. Do NOT log or expose in error messages (FR-036).
        /// </remarks>
        public string Token { get; set; }

        /// <summary>
        /// Builds the configuration provider instance.
        /// </summary>
        /// <param name="builder">The configuration builder</param>
        /// <returns>Configuration provider instance</returns>
        /// <remarks>
        /// Called by Microsoft.Extensions.Configuration infrastructure during
        /// configuration building. Creates provider that will scan and resolve
        /// op:// URIs when Load() is called.
        /// </remarks>
        public IConfigurationProvider Build(IConfigurationBuilder builder);
    }

    /// <summary>
    /// Configuration provider that resolves 1Password op:// URIs to actual secret values.
    /// </summary>
    /// <remarks>
    /// Internal interface (implementation detail). Not exposed in public API.
    ///
    /// Lifecycle (per spec requirements):
    /// 1. Constructed by OnePasswordConfigurationSource
    /// 2. Load() called by configuration system (FR-015, FR-016)
    ///    - Scans all existing config keys for op:// URIs
    ///    - Validates all URIs (fail fast on malformed) (FR-026, FR-028, FR-029, FR-031, FR-032)
    ///    - Batch retrieves all secrets (FR-018, FR-019)
    ///    - Caches resolved values (FR-020)
    /// 3. Configuration finalized (secrets now cached and immutable)
    /// 4. Application reads config (returns cached secrets)
    ///
    /// Error Handling:
    /// - All op:// URIs are REQUIRED (no optional secrets) (FR-027, FR-030)
    /// - Any failure during Load() fails the entire configuration build (fail-fast)
    /// - Malformed URIs fail before API calls (FR-028, FR-031)
    /// - Network errors retry 3x with exponential backoff (FR-033)
    /// - Batch size limit: 100 secrets (FR-022)
    /// - Timeout: 10 seconds (FR-024)
    ///
    /// Security:
    /// - Cached secrets stored in memory only (no disk persistence) (FR-035)
    /// - Secrets never logged (FR-039, FR-040, FR-043)
    /// - Error messages sanitized (no partial secret values) (FR-038)
    /// </remarks>
    internal interface IOnePasswordConfigurationProvider : IConfigurationProvider
    {
        // Inherits from IConfigurationProvider:
        // - void Load()                      // Scan config, resolve op:// URIs, cache secrets
        // - bool TryGet(string key, out string value)   // Return cached secret or original value
        // - void Set(string key, string value)          // Not used (read-only provider)
        // - IEnumerable<string> GetChildKeys(...)       // Standard config traversal
    }
}
