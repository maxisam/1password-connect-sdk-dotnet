# Example: Configuration Integration

This example demonstrates the **recommended** approach for using the 1Password .NET SDK: automatic resolution of `op://` URIs in `appsettings.json`.

## What This Example Shows

- **Automatic secret resolution** at application startup
- **Zero code changes** to access secrets (just use `IConfiguration`)
- **Environment variable override** for local development and testing
- **Seamless integration** with Microsoft.Extensions.Configuration
- **Production-ready patterns** for managing secrets

## How It Works

1. Add `op://` URIs to your `appsettings.json`
2. Add `.AddOnePassword()` to your configuration builder
3. Use `IConfiguration` normally - secrets are automatically resolved!

## Running the Example

### Prerequisites

- .NET 8.0 SDK
- 1Password Connect server
- Valid access token

### Setup

1. **Edit `appsettings.json`** to set your 1Password Connect credentials:

```json
{
  "OnePassword": {
    "ConnectServer": "https://localhost:8080",
    "Token": "your-access-token-here"
  }
}
```

**Security Note**: In production, use environment variables instead:

```bash
export OnePassword__ConnectServer="https://connect.production.com"
export OnePassword__Token="your-production-token"
```

2. **Run the example**:

```bash
cd examples/ConfigurationIntegration/ConfigurationIntegration
dotnet build
dotnet run
```

## Configuration File

The `appsettings.json` contains a mix of secrets (`op://` URIs) and regular configuration:

```json
{
  "ConnectionStrings": {
    "Database": "op://production/database/connection_string"
  },
  "ExternalServices": {
    "Stripe": {
      "SecretKey": "op://production/api-keys/stripe/secret_key",
      "PublishableKey": "pk_test_12345"  // Not a secret, plain text OK
    }
  },
  "AppSettings": {
    "Environment": "Production",  // Regular config, not a secret
    "LogLevel": "Information"
  }
}
```

## Environment Variable Override

Override any secret for local development:

```bash
# Override database connection with local test database
export ConnectionStrings__Database="Server=localhost;Database=test;User=test;Password=test"

# Override Stripe key with test key
export ExternalServices__Stripe__SecretKey="sk_test_12345"
```

Run the app - environment variables automatically win over 1Password secrets!

## Expected Output

```
1Password .NET SDK - Configuration Integration Example
========================================================

Step 1: Building configuration from appsettings.json
-----------------------------------------------------
✅ Configuration built successfully!

Step 2: Accessing configuration values
---------------------------------------

📊 Connection Strings:
  Database: Serv***ring
  Redis:    redi***1234

🔑 External Services:
  Stripe Secret:        sk_t***abcd
  Stripe Publishable:   pk_test_12345 (public key, not secret)
  SendGrid API Key:     SG.a***xyz
  Twilio Account SID:   AC12***3456
  Twilio Auth Token:    auth***5678

⚙️  App Settings (non-secrets):
  Environment:          Production
  Log Level:            Information
  Beta Features:        False
  Analytics:            True

Step 3: Environment Variable Override
-------------------------------------
You can override any secret with an environment variable:

Example:
  export ConnectionStrings__Database="Server=localhost;..."
  export ExternalServices__Stripe__SecretKey="sk_test_12345"

The environment variable will automatically override the 1Password secret!
This is perfect for local development or CI/CD testing.

Step 4: Typical Usage Patterns
-------------------------------

In a real application, you would use these values like:

  // Database connection
  var connectionString = configuration["ConnectionStrings:Database"];
  using var connection = new SqlConnection(connectionString);

  // Stripe payment
  var stripeKey = configuration["ExternalServices:Stripe:SecretKey"];
  StripeConfiguration.ApiKey = stripeKey;

  // SendGrid email
  var sendGridKey = configuration["ExternalServices:SendGrid:ApiKey"];
  var client = new SendGridClient(sendGridKey);

📋 Summary
----------
✅ All op:// URIs were automatically resolved to actual values
✅ Secrets are loaded once at application startup
✅ Secrets are cached in memory for the application lifetime
✅ Environment variables can override secrets for testing
✅ No manual secret management code required!

💡 Pro Tips:
  1. Store 1Password credentials in environment variables (not appsettings.json)
  2. Use appsettings.Development.json for local dev overrides
  3. Add environment-specific configuration files
  4. Never commit secrets to source control!
```

## Key Code

### Configuration Builder

```csharp
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()  // Can override secrets
    .AddOnePassword()            // ⭐ Resolves op:// URIs
    .Build();
```

### Accessing Secrets

```csharp
// No special code needed - just use IConfiguration!
var dbConnection = configuration["ConnectionStrings:Database"];
var stripeKey = configuration["ExternalServices:Stripe:SecretKey"];
```

Secrets are automatically resolved from 1Password. You don't need to call any 1Password APIs directly!

## Best Practices Demonstrated

1. **Separation of Concerns**: Configuration structure remains clean and standard
2. **Environment-Specific Overrides**: Use environment variables for testing
3. **Security First**: Secrets never logged or exposed
4. **Fail Fast**: Invalid configuration fails at startup, not runtime
5. **Production Ready**: Works with ASP.NET Core, hosted services, workers

## Comparison with Programmatic Access

| Feature | Configuration Integration | Programmatic Access |
|---------|--------------------------|---------------------|
| Code Required | 1 line (`.AddOnePassword()`) | ~10 lines per secret |
| When Secrets Retrieved | Startup (once) | On-demand |
| Caching | Automatic | Manual |
| Environment Override | Automatic | Manual |
| Best For | Most applications | Custom secret rotation |

**Recommendation**: Use Configuration Integration for 99% of use cases.

## Next Steps

- See [AspNetCoreIntegration](../AspNetCoreIntegration/) for ASP.NET Core example
- See [ProgrammaticAccess](../ProgrammaticAccess/) for direct SDK usage
- Read the [Quickstart Guide](../../specs/001-onepassword-sdk/quickstart.md) for more patterns

## Troubleshooting

### "OnePassword:ConnectServer not configured"

**Solution**: Set the environment variable or add to `appsettings.json`:

```bash
export OnePassword__ConnectServer="https://localhost:8080"
export OnePassword__Token="your-token"
```

### Secrets not resolving

**Check**:
1. `op://` URIs are correctly formatted
2. Service account has access to the vault
3. Items and fields exist in 1Password
4. Connect server is accessible

### Environment variables not overriding

**Check**:
1. Environment variables use `__` (double underscore) as separator
2. `.AddEnvironmentVariables()` is called **before** `.AddOnePassword()`
3. Environment variable names match configuration keys exactly
