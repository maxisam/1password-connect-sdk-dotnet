# 1Password .NET SDK

[![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%208.0-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![NuGet](https://img.shields.io/badge/nuget-1.0.0--beta1-orange)](https://nuget.org)

**Seamlessly integrate 1Password secrets into your .NET applications** with automatic resolution of `op://` URIs in configuration files.

## Features

✅ **Programmatic secret retrieval** - Direct access to 1Password vaults, items, and fields
✅ **Configuration provider** - Automatically resolve `op://` URIs in `appsettings.json`
✅ **Resilience & reliability** - Automatic retry with exponential backoff and circuit breaker
✅ **Dependency injection** - First-class support for ASP.NET Core DI containers
✅ **Environment variable override** - Local development without production secrets
✅ **Batch operations** - Retrieve up to 100 secrets in a single call
✅ **Comprehensive logging** - Structured logging with correlation IDs (never logs secrets)
✅ **Multi-targeting** - Supports .NET 6.0 and .NET 8.0

## Quick Start

### Installation

```bash
# Core SDK for programmatic access
dotnet add package OnePassword.Sdk

# Configuration provider (recommended for most apps)
dotnet add package OnePassword.Configuration
```

### Configuration Integration (10 seconds)

**1. Add `op://` URIs to your `appsettings.json`:**

```json
{
  "ConnectionStrings": {
    "Database": "op://production/database/connection_string"
  },
  "ExternalServices": {
    "Stripe": {
      "SecretKey": "op://production/api-keys/stripe/secret_key"
    }
  },
  "OnePassword": {
    "ConnectServer": "https://connect.example.com",
    "Token": "your-token-here"
  }
}
```

**2. Add the 1Password provider to your configuration:**

```csharp
using OnePassword.Configuration;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .AddOnePassword()  // ⭐ Automatically resolves op:// URIs
    .Build();
```

**Done!** Secrets are automatically resolved at application startup.

### Programmatic Access

**Option 1: Dependency Injection (Recommended for ASP.NET Core)**

```csharp
using OnePassword.Sdk.Extensions;

// In Program.cs
builder.Services.AddOnePasswordClient(options =>
{
    options.ConnectServer = configuration["OnePassword:ConnectServer"];
    options.Token = configuration["OnePassword:Token"];
    options.MaxRetries = 5;  // Customize resilience settings
});

// Inject into your services
public class MyService
{
    private readonly IOnePasswordClient _client;

    public MyService(IOnePasswordClient client)
    {
        _client = client;
    }

    public async Task<string> GetSecretAsync()
    {
        return await _client.GetSecretAsync("vault", "item", "field");
    }
}
```

**Option 2: Manual Instantiation (Backward Compatible)**

```csharp
using OnePassword.Sdk;

var options = new OnePasswordClientOptions
{
    ConnectServer = "https://connect.example.com",
    Token = "your-token-here"
};

using var client = new OnePasswordClient(options);

// Retrieve a single secret
var dbPassword = await client.GetSecretAsync(
    vaultId: "production",
    itemId: "database",
    fieldLabel: "password");

// Batch retrieve multiple secrets
var secrets = await client.GetSecretsAsync(new[]
{
    "op://prod/db/password",
    "op://prod/api-keys/stripe",
    "op://prod/api-keys/sendgrid"
});
```

## Resilience & Reliability

The SDK automatically handles transient failures with **Polly v8** resilience policies:

🔄 **Automatic Retry**
- Exponential backoff with jitter (prevents thundering herd)
- Configurable retry count (default: 3)
- Only retries transient errors (503, timeouts, network issues)
- Never retries permanent errors (401, 403, 404)

⚡ **Circuit Breaker**
- Fails fast when API is consistently down
- Prevents cascading failures
- Automatic recovery testing
- Configurable thresholds

⏱️ **Timeout Management**
- Per-request timeouts (default: 10s)
- Timeout budget tracking across retries
- Graceful shutdown with in-flight request handling

📊 **Observability**
- Structured logging for all resilience events
- Warning-level logs for retry attempts and circuit state changes
- Never logs secrets or sensitive data
- Integration-ready for Application Insights, DataDog, etc.

**Configuration Example:**
```csharp
services.AddOnePasswordClient(options =>
{
    options.ConnectServer = "https://connect.example.com";
    options.Token = "your-token";

    // Retry configuration
    options.MaxRetries = 5;
    options.RetryBaseDelay = TimeSpan.FromMilliseconds(500);
    options.RetryMaxDelay = TimeSpan.FromSeconds(30);

    // Circuit breaker configuration
    options.CircuitBreakerFailureThreshold = 3;
    options.CircuitBreakerBreakDuration = TimeSpan.FromSeconds(20);
});
```

See **[Resilience Guide](specs/002-httpclient-factory-polly/quickstart.md)** for detailed configuration options.

## Documentation

- **[Quickstart Guide](specs/001-onepassword-sdk/quickstart.md)** - Complete walkthrough with examples
- **[Resilience Guide](specs/002-httpclient-factory-polly/quickstart.md)** - Retry, circuit breaker, and DI configuration
- **[API Reference](specs/001-onepassword-sdk/contracts/README.md)** - Detailed API documentation
- **[Configuration Guide](specs/001-onepassword-sdk/quickstart.md#use-case-2-configuration-integration-recommended)** - How to use the configuration provider
- **[Environment Override](specs/001-onepassword-sdk/quickstart.md#step-4-override-secrets-with-environment-variables)** - Local development best practices

## Use Cases

### ASP.NET Core Integration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add 1Password configuration provider
builder.Configuration.AddOnePassword();

// Secrets are now available throughout the app
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration["ConnectionStrings:Database"]));

var app = builder.Build();
app.Run();
```

### Local Development Override

Override production secrets with local values using environment variables:

```bash
export ConnectionStrings__Database="Server=localhost;Database=dev;..."
export ExternalServices__Stripe__SecretKey="sk_test_12345"
```

No code changes needed! Environment variables automatically override 1Password secrets.

## Prerequisites

- **.NET 6.0** or **.NET 8.0** SDK
- **1Password Connect Server** - [Setup Guide](https://developer.1password.com/docs/connect/)
- **Access Token** - [Create Token](https://developer.1password.com/docs/connect/manage-connect#create-a-token)

## Security Best Practices

✅ **DO:**
- Store authentication token in environment variables in production
- Use HTTPS for Connect server URL (enforced by SDK)
- Keep access tokens secure (never commit to source control)
- Use least-privilege access (grant service account only necessary vault permissions)

❌ **DON'T:**
- Log secret values (SDK prevents this automatically)
- Hardcode tokens in application code
- Commit `appsettings.json` with tokens to source control
- Share access tokens across environments

## Building and Testing

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run all tests
dotnet test

# Run specific test project
dotnet test tests/OnePassword.Sdk.Tests
```

### Test Coverage

```
✅ 117+ passing tests
   - 71 SDK unit tests (including resilience policies)
   - 21 Configuration provider tests
   - 25 Integration tests (DI, backward compatibility, resilience)
   - 7 tests skipped (WireMock placeholders)

Code Coverage: ~75% line coverage
   - Target: >80% for core logic
   - Resilience features fully tested
```

Generate coverage reports:

```bash
# Windows (PowerShell)
.\coverage.ps1

# Unix/Linux/macOS
chmod +x coverage.sh
./coverage.sh
```

This generates an HTML report at `TestResults/CoverageReport/index.html` showing detailed coverage metrics for all source files.

## Examples

Three complete example projects demonstrate different integration patterns:

### 1. ProgrammaticAccess
Direct SDK usage for fetching secrets programmatically:
```bash
cd examples/ProgrammaticAccess
dotnet run
```
[View Example](examples/ProgrammaticAccess)

### 2. ConfigurationIntegration
Console application with automatic op:// URI resolution from appsettings.json:
```bash
cd examples/ConfigurationIntegration
dotnet run
```
[View Example](examples/ConfigurationIntegration)

### 3. AspNetCoreIntegration
ASP.NET Core Web API with 1Password secrets for database connections and external services:
```bash
cd examples/AspNetCoreIntegration
dotnet run
# Open https://localhost:5001/swagger
```
[View Example](examples/AspNetCoreIntegration)

## Project Structure

```
src/
├── OnePassword.Sdk/           # Core SDK library
├── OnePassword.Configuration/ # Configuration provider
tests/
├── OnePassword.Sdk.Tests/           # SDK unit tests
├── OnePassword.Configuration.Tests/ # Provider unit tests
├── OnePassword.Integration.Tests/   # Integration tests
specs/
└── 001-onepassword-sdk/       # Design docs, quickstart, API contracts
```

## Contributing

Contributions are welcome! Please:

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Commit** your changes (`git commit -m 'Add amazing feature'`)
4. **Push** to the branch (`git push origin feature/amazing-feature`)
5. **Open** a Pull Request

### Development Workflow

1. Make your changes
2. Run tests: `dotnet test`
3. Ensure code builds without warnings
4. Update documentation if needed
5. Submit PR with clear description

## Versioning

We use [SemVer](https://semver.org/) for versioning. Current version: **1.0.0-beta1**

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- **Issues**: [GitHub Issues](https://github.com/maxisam/dotnet-1password/issues)
- **Documentation**: [1Password Developer Docs](https://developer.1password.com/docs/)
- **Community**: [1Password Support](https://support.1password.com/)

## Acknowledgments

- Built with [1Password Connect API](https://developer.1password.com/docs/connect/)
- Uses [Microsoft.Extensions.Configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- Inspired by the .NET community's security-first approach

---

**Made with ❤️ for the .NET community**
