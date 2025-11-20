# Quickstart Guide: 1Password .NET SDK

**Feature**: 001-onepassword-sdk
**Target Audience**: .NET developers integrating 1Password secrets into their applications
**Estimated Time**: 10 minutes

## Overview

The 1Password .NET SDK enables you to:
1. **Programmatically access** 1Password vaults and retrieve secrets in your .NET code
2. **Automatically resolve secrets** in configuration files (appsettings.json) using `op://` URIs

This guide walks you through both use cases with minimal setup.

---

## Prerequisites

Before starting, ensure you have:

1. **1Password Connect Server**: Self-hosted 1Password Connect server (see [1Password Connect docs](https://developer.1password.com/docs/connect/))
2. **Access Token**: Generated from 1Password https://developer.1password.com/docs/connect/manage-connect#create-a-token
3. **.NET 8.0 SDK** or later
4. **Network Access**: Your application can reach the Connect server URL

---

## Installation

Install the SDK packages via NuGet:

```bash
# Core SDK for programmatic access
dotnet add package OnePassword.Sdk

# Configuration provider for appsettings.json integration (optional)
dotnet add package OnePassword.Configuration
```

---

## Use Case 1: Programmatic Secret Retrieval

**Scenario**: You need to retrieve a database password from 1Password in your application code.

### Step 1: Configure Authentication

Store your Connect server details in `appsettings.json`:

```json
{
  "OnePassword": {
    "ConnectServer": "https://connect.example.com",
    "Token": "your-service-account-token-here"
  }
}
```

**Security Note**: Do NOT commit `Token` to source control. Use environment variables in production:
```bash
export OnePassword__Token="your-token-here"
```

### Step 2: Initialize the Client

```csharp
using OnePassword.Sdk;
using Microsoft.Extensions.Configuration;

// Load configuration
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

// Create client options from configuration
var options = new OnePasswordClientOptions
{
    ConnectServer = configuration["OnePassword:ConnectServer"],
    Token = configuration["OnePassword:Token"]
};

// Initialize client
using var client = new OnePasswordClient(options);
```

### Step 3: Retrieve a Secret

```csharp
// Get a specific field from an item
var dbPassword = await client.GetSecretAsync(
    vaultId: "production",
    itemId: "database-credentials",
    fieldLabel: "password");

Console.WriteLine("Database password retrieved successfully!");
// Do NOT log the actual password value
```

### Step 4: Handle Errors

```csharp
using OnePassword.Sdk.Exceptions;

try
{
    var apiKey = await client.GetSecretAsync("prod", "api-keys", "stripe_secret");
}
catch (FieldNotFoundException ex)
{
    Console.WriteLine($"Field '{ex.FieldLabel}' not found. Check the field name in 1Password.");
}
catch (AuthenticationException)
{
    Console.WriteLine("Authentication failed. Verify your token.");
}
catch (NetworkException ex)
{
    Console.WriteLine($"Network error after {ex.RetryAttempts} retries. Check connectivity.");
}
```

**Complete Example**: See [examples/programmatic-access](../examples/programmatic-access)

---

## Use Case 2: Configuration Integration (Recommended)

**Scenario**: You have multiple secrets referenced in `appsettings.json` and want them automatically resolved at startup.

### Step 1: Add op:// URIs to Configuration

Edit your `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Database": "op://production/database/connection_string"
  },
  "ExternalServices": {
    "Stripe": {
      "SecretKey": "op://production/api-keys/stripe/secret_key"
    },
    "SendGrid": {
      "ApiKey": "op://production/api-keys/sendgrid/api_key"
    }
  },
  "OnePassword": {
    "ConnectServer": "https://connect.example.com",
    "Token": "your-token-here"
  }
}
```

### Step 2: Add 1Password Provider to Configuration Builder

```csharp
using Microsoft.Extensions.Configuration;
using OnePassword.Configuration;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()  // Environment variables override secrets
    .AddOnePassword()            // ⭐ Automatically resolves op:// URIs
    .Build();
```

That's it! When `Build()` is called, the 1Password provider:
1. Scans all configuration for `op://` URIs
2. Retrieves all secrets in a single batch call
3. Replaces URIs with actual secret values
4. Caches secrets in memory

### Step 3: Use Configuration Normally

```csharp
// Read resolved secrets as normal configuration values
var dbConnectionString = configuration["ConnectionStrings:Database"];
var stripeKey = configuration["ExternalServices:Stripe:SecretKey"];

// Secrets are automatically resolved - no op:// URIs returned!
Console.WriteLine("Configuration loaded with secrets resolved!");
```

### Step 4: Override Secrets with Environment Variables

Environment variables automatically override 1Password secrets, making it easy to test locally or run in CI/CD without accessing production secrets.

#### How It Works

The configuration builder precedence (last added wins) means environment variables override 1Password secrets:

```csharp
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")      // Contains op:// URIs
    .AddEnvironmentVariables()             // Can override with real values
    .AddOnePassword()                      // Resolves remaining op:// URIs
    .Build();
```

**Important**: Add `AddEnvironmentVariables()` **before** `AddOnePassword()` to enable overrides.

#### Example: Local Development Override

**appsettings.json** (committed to source control):
```json
{
  "ConnectionStrings": {
    "Database": "op://production/database/connection_string"
  },
  "ExternalServices": {
    "Stripe": {
      "SecretKey": "op://production/api-keys/stripe/secret_key"
    }
  }
}
```

**Local environment variables** (your machine only):
```bash
# Override production database with local test database
export ConnectionStrings__Database="Server=localhost;Database=test;User=test;Password=test"

# Override production Stripe key with test key
export ExternalServices__Stripe__SecretKey="sk_test_12345"
```

When you run the application locally:
- `ConnectionStrings:Database` returns your local database connection (from env var, **not** from 1Password)
- `ExternalServices:Stripe:SecretKey` returns your test Stripe key (from env var, **not** from 1Password)

No changes to code required! The 1Password provider automatically skips resolving op:// URIs that have been overridden by environment variables.

#### Example: CI/CD Override

In your CI/CD pipeline, override secrets for integration tests:

**GitHub Actions example**:
```yaml
- name: Run integration tests
  run: dotnet test
  env:
    ConnectionStrings__Database: "Server=testdb;Database=ci_test;..."
    ExternalServices__Stripe__SecretKey: "sk_test_ci_12345"
```

The application runs normally without accessing production 1Password secrets.

**Complete Example**: See [examples/configuration-integration](../examples/configuration-integration)

---

## Use Case 3: ASP.NET Core Integration

**Scenario**: Integrate 1Password secrets into an ASP.NET Core application.

### Step 1: Configure in Program.cs

```csharp
using OnePassword.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add 1Password configuration provider
builder.Configuration.AddOnePassword();

// Secrets are now available throughout the app
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration["ConnectionStrings:Database"]));

var app = builder.Build();
app.Run();
```

### Step 2: Inject Configuration in Controllers

```csharp
public class PaymentController : ControllerBase
{
    private readonly string _stripeKey;

    public PaymentController(IConfiguration configuration)
    {
        // Secret automatically resolved from op:// URI
        _stripeKey = configuration["ExternalServices:Stripe:SecretKey"];
    }

    [HttpPost("charge")]
    public async Task<IActionResult> CreateCharge()
    {
        // Use _stripeKey for Stripe API calls
        // ...
    }
}
```

**Complete Example**: See [examples/aspnetcore-integration](../examples/aspnetcore-integration)

---

## Common Scenarios

### Batch Retrieve Multiple Secrets

If you need to fetch many secrets programmatically (not via configuration):

```csharp
var references = new[]
{
    "op://prod/database/password",
    "op://prod/api-keys/stripe/secret",
    "op://prod/api-keys/sendgrid/api_key"
};

var secrets = await client.GetSecretsAsync(references);

foreach (var (uri, value) in secrets)
{
    Console.WriteLine($"{uri} resolved successfully");
    // Do NOT log the actual secret value
}
```

**Limits**: Maximum 100 secrets per batch, 10-second timeout, 1MB per secret.

---

### Handle Missing Secrets Gracefully

By default, missing secrets fail the application startup (fail-fast). For advanced scenarios where you want custom handling:

```csharp
try
{
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddOnePassword()
        .Build();
}
catch (FieldNotFoundException ex)
{
    Console.WriteLine($"Secret not found: {ex.VaultId}/{ex.ItemId}/{ex.FieldLabel}");
    Console.WriteLine("Ensure the secret exists in 1Password and the service account has access.");
    Environment.Exit(1);
}
catch (MalformedUriException ex)
{
    Console.WriteLine($"Invalid op:// URI in config key '{ex.ConfigurationKey}': {ex.Message}");
    Environment.Exit(1);
}
```

---

### Development vs. Production Configuration

Use different configurations for development and production:

**appsettings.Development.json** (local development):
```json
{
  "ConnectionStrings": {
    "Database": "Server=localhost;Database=dev;User=dev;Password=dev"
  }
}
```

**appsettings.Production.json** (production):
```json
{
  "ConnectionStrings": {
    "Database": "op://production/database/connection_string"
  },
  "OnePassword": {
    "ConnectServer": "https://connect.production.example.com"
  }
}
```

The 1Password provider only activates when `OnePassword:ConnectServer` is configured.

---

## Performance Tips

### 1. Minimize Secrets at Startup
The SDK retrieves secrets during application startup. Keep secret count reasonable (<20) for fast startup (<500ms overhead).

### 2. Use Batch Retrieval
For programmatic access, use `GetSecretsAsync()` instead of multiple `GetSecretAsync()` calls to minimize HTTP requests.

### 3. Cache Secrets Yourself (If Needed)
Configuration provider caches secrets automatically. For programmatic access, cache retrieved secrets in your application if accessed frequently.

---

## Security Best Practices

### ✅ DO:
- Store authentication token in environment variables in production
- Use HTTPS for Connect server URL (enforced by SDK)
- Keep access tokens secure (never commit to source control)
- Use least-privilege access (grant service account only necessary vault permissions)
- Monitor token expiration (tokens don't auto-refresh)

### ❌ DON'T:
- Log secret values (SDK prevents this, but be cautious in your code)
- Hardcode tokens in application code
- Commit appsettings.json with tokens to source control
- Share access tokens across environments (dev, staging, prod)

---

## Troubleshooting

### Problem: "Authentication failed" error

**Cause**: Invalid or expired token.

**Solution**:
1. Verify token in environment variable or appsettings.json
2. Check token hasn't expired in 1Password
3. Regenerate token if necessary

---

### Problem: "Vault not found" error

**Cause**: Service account doesn't have permission to access the vault.

**Solution**:
1. Verify vault name is correct (case-sensitive)
2. Check service account permissions in 1Password Connect
3. Grant service account read access to the vault

---

### Problem: "Field not found" error

**Cause**: Field label doesn't match the field name in 1Password.

**Solution**:
1. Check field label is correct (case-sensitive)
2. View item in 1Password to verify field name
3. Use exact field label from 1Password (e.g., "password", not "Password")

---

### Problem: "Malformed op:// URI" error

**Cause**: Invalid syntax in op:// URI.

**Solution**:
1. Check URI format: `op://<vault>/<item>/<field>` or `op://<vault>/<item>/<section>/<field>`
2. URL-encode special characters (spaces, slashes): `op://vault/my%20item/password`
3. Verify no empty components (e.g., `op://vault//field` is invalid)

---

### Problem: Network timeout or connection refused

**Cause**: Cannot reach 1Password Connect server.

**Solution**:
1. Verify Connect server URL is correct
2. Check network connectivity: `curl https://connect.example.com/health`
3. Verify firewall allows outbound HTTPS to Connect server
4. Check Connect server is running

---

## Next Steps

- **Deep Dive**: Read the [API Reference](../contracts/README.md) for detailed API documentation
- **Examples**: Explore complete examples in [examples/](../examples/)
- **Best Practices**: Review [1Password Connect Security Best Practices](https://developer.1password.com/docs/connect/security/)
- **Testing**: Learn how to mock the SDK for unit testing in [testing-guide.md](../testing-guide.md)

---

## Getting Help

- **Issues**: Report bugs at [GitHub Issues](https://github.com/your-repo/dotnet-1password/issues)
- **Documentation**: [1Password Developer Docs](https://developer.1password.com/docs/)
- **API Reference**: See [contracts/README.md](../contracts/README.md)

---

**Estimated Reading Time**: 10 minutes
**Skill Level**: Intermediate .NET developer
**Last Updated**: 2025-11-18
