// Example: Configuration Integration with 1Password
// Demonstrates automatic op:// URI resolution in appsettings.json

using System.Reflection;
using Microsoft.Extensions.Configuration;
using OnePassword.Configuration;

Console.WriteLine("1Password .NET SDK - Configuration Integration Example");
Console.WriteLine("========================================================\n");

// Step 1: Build configuration with 1Password provider
Console.WriteLine("Step 1: Building configuration from appsettings.json");
Console.WriteLine("-----------------------------------------------------");

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
    // Add environment variables first so they can supply credentials to the 1Password provider
    .AddEnvironmentVariables()
    // Add user-secrets so local secrets (e.g. connect URL / token) are available when AddOnePassword runs
    .AddUserSecrets(Assembly.GetEntryAssembly()!)
    // Add the 1Password provider which will resolve op:// URIs using credentials from above
    .AddOnePassword()
    // Re-add environment variables so they take precedence over values resolved from 1Password
    .AddEnvironmentVariables()
    .Build();

Console.WriteLine("✅ Configuration built successfully!\n");

// Step 2: Access configuration values
Console.WriteLine("Step 2: Accessing configuration values");
Console.WriteLine("---------------------------------------");

// Connection strings
Console.WriteLine("\n📊 Connection Strings:");
Console.WriteLine($"  Database: {MaskSecret(configuration["ConnectionStrings:Database"])}");
Console.WriteLine($"  Redis:    {MaskSecret(configuration["ConnectionStrings:Redis"])}");

// External service API keys
Console.WriteLine("\n🔑 External Services:");
Console.WriteLine($"  Stripe Secret:        {MaskSecret(configuration["ExternalServices:Stripe:SecretKey"])}");
Console.WriteLine($"  Stripe Publishable:   {configuration["ExternalServices:Stripe:PublishableKey"]} (public key, not secret)");
Console.WriteLine($"  SendGrid API Key:     {MaskSecret(configuration["ExternalServices:SendGrid:ApiKey"])}");
Console.WriteLine($"  Twilio Account SID:   {MaskSecret(configuration["ExternalServices:Twilio:AccountSid"])}");
Console.WriteLine($"  Twilio Auth Token:    {MaskSecret(configuration["ExternalServices:Twilio:AuthToken"])}");

// Regular configuration values (non-secrets)
Console.WriteLine("\n⚙️  App Settings (non-secrets):");
Console.WriteLine($"  Environment:          {configuration["AppSettings:Environment"]}");
Console.WriteLine($"  Log Level:            {configuration["AppSettings:LogLevel"]}");
Console.WriteLine($"  Beta Features:        {configuration["AppSettings:FeatureFlags:EnableBetaFeatures"]}");
Console.WriteLine($"  Analytics:            {configuration["AppSettings:FeatureFlags:EnableAnalytics"]}");

// Step 3: Demonstrate environment variable override
Console.WriteLine("\n\nStep 3: Environment Variable Override");
Console.WriteLine("-------------------------------------");
Console.WriteLine("You can override any secret with an environment variable:");
Console.WriteLine();
Console.WriteLine("Example:");
Console.WriteLine("  export ConnectionStrings__Database=\"Server=localhost;...\"");
Console.WriteLine("  export ExternalServices__Stripe__SecretKey=\"sk_test_12345\"");
Console.WriteLine();
Console.WriteLine("The environment variable will automatically override the 1Password secret!");
Console.WriteLine("This is perfect for local development or CI/CD testing.");

// Step 4: Show typical usage patterns
Console.WriteLine("\n\nStep 4: Typical Usage Patterns");
Console.WriteLine("-------------------------------");
Console.WriteLine();
Console.WriteLine("In a real application, you would use these values like:");
Console.WriteLine();
Console.WriteLine("  // Database connection");
Console.WriteLine("  var connectionString = configuration[\"ConnectionStrings:Database\"];");
Console.WriteLine("  using var connection = new SqlConnection(connectionString);");
Console.WriteLine();
Console.WriteLine("  // Stripe payment");
Console.WriteLine("  var stripeKey = configuration[\"ExternalServices:Stripe:SecretKey\"];");
Console.WriteLine("  StripeConfiguration.ApiKey = stripeKey;");
Console.WriteLine();
Console.WriteLine("  // SendGrid email");
Console.WriteLine("  var sendGridKey = configuration[\"ExternalServices:SendGrid:ApiKey\"];");
Console.WriteLine("  var client = new SendGridClient(sendGridKey);");

// Summary
Console.WriteLine("\n\n📋 Summary");
Console.WriteLine("----------");
Console.WriteLine("✅ All op:// URIs were automatically resolved to actual values");
Console.WriteLine("✅ Secrets are loaded once at application startup");
Console.WriteLine("✅ Secrets are cached in memory for the application lifetime");
Console.WriteLine("✅ Environment variables can override secrets for testing");
Console.WriteLine("✅ No manual secret management code required!");

Console.WriteLine("\n\n💡 Pro Tips:");
Console.WriteLine("  1. Store 1Password credentials in environment variables (not appsettings.json)");
Console.WriteLine("  2. Use appsettings.Development.json for local dev overrides");
Console.WriteLine("  3. Add environment-specific configuration files");
Console.WriteLine("  4. Never commit secrets to source control!");

Console.WriteLine("\n\nExample completed. Press any key to exit...");
Console.ReadKey();

// Helper function to mask secret values for display
static string MaskSecret(string? value)
{
    if (string.IsNullOrEmpty(value))
    {
        return "(not set)";
    }

    // Show first and last 4 characters, mask the rest
    if (value.Length <= 8)
    {
        return "***" + value.Substring(value.Length - 2);
    }

    return value.Substring(0, 4) + "***" + value.Substring(value.Length - 4);
}
