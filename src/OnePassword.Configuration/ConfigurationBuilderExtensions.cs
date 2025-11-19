// API Contract: ConfigurationBuilderExtensions
// Feature: 001-onepassword-sdk

using Microsoft.Extensions.Configuration;

namespace OnePassword.Configuration;

/// <summary>
/// Extension methods for adding 1Password configuration source to IConfigurationBuilder.
/// </summary>
/// <remarks>
/// Provides the primary public API for developers to integrate 1Password secrets
/// into their .NET configuration. Designed for "pit of success" developer experience.
///
/// Usage example:
/// <code>
/// var configuration = new ConfigurationBuilder()
///     .AddJsonFile("appsettings.json")
///     .AddEnvironmentVariables()
///     .AddOnePassword()  // <-- Adds 1Password provider
///     .Build();
/// </code>
///
/// Corresponds to Success Criterion SC-001: Less than 10 lines of code for integration
/// </remarks>
public static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds 1Password configuration source using credentials from existing configuration.
    /// </summary>
    /// <param name="builder">The configuration builder</param>
    /// <returns>The configuration builder for chaining</returns>
    /// <exception cref="ArgumentNullException">builder is null</exception>
    /// <exception cref="InvalidOperationException">
    /// OnePassword:ConnectServer or OnePassword:Token not found in configuration (FR-006, FR-007)
    /// </exception>
    /// <remarks>
    /// This overload reads authentication credentials from configuration sources already
    /// added to the builder. Expected configuration keys:
    /// - OnePassword:ConnectServer (or OnePassword__ConnectServer env var)
    /// - OnePassword:Token (or OnePassword__Token env var)
    ///
    /// Environment variables take precedence over appsettings.json (FR-007).
    ///
    /// Typical usage:
    /// <code>
    /// var configuration = new ConfigurationBuilder()
    ///     .AddJsonFile("appsettings.json")       // Contains OnePassword:ConnectServer, OnePassword:Token
    ///     .AddEnvironmentVariables()             // Can override with OnePassword__ConnectServer, OnePassword__Token
    ///     .AddOnePassword()                       // Reads credentials from above sources
    ///     .Build();
    /// </code>
    ///
    /// Corresponds to FR-006, FR-007: SDK reads authentication from appsettings.json
    /// or environment variables, with environment variables taking precedence.
    /// </remarks>
    public static IConfigurationBuilder AddOnePassword(this IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Build temporary configuration to read OnePassword settings
        var tempConfig = builder.Build();

        var connectServer = tempConfig["OnePassword:ConnectServer"];
        var token = tempConfig["OnePassword:Token"];

        return AddOnePassword(builder, options =>
        {
            options.ConnectServer = connectServer ?? string.Empty;
            options.Token = token ?? string.Empty;
        });
    }

    /// <summary>
    /// Adds 1Password configuration source with explicit credentials.
    /// </summary>
    /// <param name="builder">The configuration builder</param>
    /// <param name="connectServer">1Password Connect server URL (must be HTTPS)</param>
    /// <param name="token">Access token for authentication</param>
    /// <returns>The configuration builder for chaining</returns>
    /// <exception cref="ArgumentNullException">builder, connectServer, or token is null</exception>
    /// <exception cref="ArgumentException">connectServer is not a valid HTTPS URL</exception>
    /// <remarks>
    /// This overload allows explicit credential configuration without relying on
    /// existing configuration sources. Useful for testing or programmatic setup.
    ///
    /// Security Warning: Hardcoding credentials in code is NOT recommended. Use the
    /// parameterless overload with appsettings.json or environment variables instead.
    ///
    /// Example usage:
    /// <code>
    /// var configuration = new ConfigurationBuilder()
    ///     .AddJsonFile("appsettings.json")
    ///     .AddOnePassword("https://connect.example.com", "your-token-here")
    ///     .Build();
    /// </code>
    /// </remarks>
    public static IConfigurationBuilder AddOnePassword(
        this IConfigurationBuilder builder,
        string connectServer,
        string token)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(connectServer);
        ArgumentNullException.ThrowIfNull(token);

        return AddOnePassword(builder, options =>
        {
            options.ConnectServer = connectServer;
            options.Token = token;
        });
    }

    /// <summary>
    /// Adds 1Password configuration source with custom configuration action.
    /// </summary>
    /// <param name="builder">The configuration builder</param>
    /// <param name="configureOptions">Action to configure OnePasswordConfigurationSource options</param>
    /// <returns>The configuration builder for chaining</returns>
    /// <exception cref="ArgumentNullException">builder or configureOptions is null</exception>
    /// <remarks>
    /// Advanced overload for fine-grained control over configuration source options.
    /// Allows setting timeout, retry policies, etc. in the future.
    ///
    /// Example usage:
    /// <code>
    /// var configuration = new ConfigurationBuilder()
    ///     .AddJsonFile("appsettings.json")
    ///     .AddOnePassword(options =>
    ///     {
    ///         options.ConnectServer = "https://connect.example.com";
    ///         options.Token = Environment.GetEnvironmentVariable("OP_TOKEN");
    ///     })
    ///     .Build();
    /// </code>
    /// </remarks>
    public static IConfigurationBuilder AddOnePassword(
        this IConfigurationBuilder builder,
        Action<OnePasswordConfigurationSource> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var source = new OnePasswordConfigurationSource();
        configureOptions(source);

        builder.Add(source);

        return builder;
    }
}
