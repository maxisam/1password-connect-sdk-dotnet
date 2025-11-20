# ASP.NET Core Integration Example

This example demonstrates how to integrate the 1Password .NET SDK into an ASP.NET Core Web API application using the configuration provider.

## What This Example Shows

This minimal API demonstrates:

1. **Automatic Secret Resolution**: Secrets stored as `op://` URIs in `appsettings.json` are automatically resolved when the application starts
2. **Configuration Integration**: Seamless integration with ASP.NET Core's configuration system
3. **Environment Variable Override**: Local development using environment variables to override production secrets
4. **Production Patterns**: Real-world usage patterns for database connections and external API credentials

## Project Structure

```
AspNetCoreIntegration/
├── Program.cs           # Main application entry point
├── appsettings.json     # Configuration with op:// URIs
└── README.md           # This file
```

## Prerequisites

- **.NET 8.0 SDK**
- **1Password Connect Server** running (default: `https://localhost:8080`)
- **Access Token** from 1Password Connect

## Configuration

The `appsettings.json` file contains op:// URIs that reference secrets in 1Password:

```json
{
  "ConnectionStrings": {
    "Database": "op://Production/Database/connection-string"
  },
  "ExternalServices": {
    "ApiKey": "op://Production/ThirdPartyAPI/api-key",
    "SecretKey": "op://Production/ThirdPartyAPI/secret-key"
  },
  "OnePassword": {
    "ConnectServer": "https://localhost:8080",
    "Token": "your-token-here"
  }
}
```

## Running the Example

### Step 1: Configure 1Password

Update `appsettings.json` with your 1Password Connect server details:

```json
{
  "OnePassword": {
    "ConnectServer": "https://your-connect-server:8080",
    "Token": "your-access-token-here"
  }
}
```

Or use environment variables (recommended):

```bash
export OnePassword__ConnectServer="https://your-connect-server:8080"
export OnePassword__Token="your-access-token"
```

### Step 2: Run the Application

```bash
cd examples/AspNetCoreIntegration
dotnet run
```

The application will start on `https://localhost:5001` (or `http://localhost:5000`).

### Step 3: Test the Endpoints

```bash
# Health check
curl https://localhost:5001/health

# Configuration status (shows which secrets are configured)
curl https://localhost:5001/config/status

# Simulate database query (uses resolved connection string)
curl https://localhost:5001/api/data

# Simulate external API call (uses resolved API credentials)
curl https://localhost:5001/api/external

# Swagger UI (interactive API documentation)
# Open in browser: https://localhost:5001/swagger
```

## Local Development Override

For local development, override production secrets with environment variables:

### Windows (PowerShell)

```powershell
$env:ConnectionStrings__Database = "Server=localhost;Database=dev;..."
$env:ExternalServices__ApiKey = "dev-api-key"
$env:ExternalServices__SecretKey = "dev-secret-key"
dotnet run
```

### macOS/Linux (Bash)

```bash
export ConnectionStrings__Database="Server=localhost;Database=dev;..."
export ExternalServices__ApiKey="dev-api-key"
export ExternalServices__SecretKey="dev-secret-key"
dotnet run
```

Environment variables automatically override the `op://` URIs from appsettings.json—no code changes needed!

## How It Works

### 1. Configuration Provider Registration

In `Program.cs`, the 1Password configuration provider is added:

```csharp
builder.Configuration.AddOnePassword();
```

This single line:
- Scans all configuration keys for `op://` URIs
- Resolves them from 1Password Connect API
- Caches the secrets for the lifetime of the application

### 2. Secret Resolution

When the application starts:

1. **Load appsettings.json** → Contains `op://` URIs
2. **Load environment variables** → May override some values
3. **Run AddOnePassword()** → Resolves remaining `op://` URIs
4. **Build configuration** → All secrets available via `IConfiguration`

### 3. Using Secrets in Code

Secrets are accessed like any other configuration value:

```csharp
var connectionString = config["ConnectionStrings:Database"];
var apiKey = config["ExternalServices:ApiKey"];
```

## Real-World Usage Patterns

### Database Connection with Entity Framework

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration["ConnectionStrings:Database"]));
```

The connection string is resolved from:
- `op://Production/Database/connection-string` in production
- Environment variable `ConnectionStrings__Database` in local dev

### External Service Configuration

```csharp
builder.Services.Configure<StripeOptions>(options =>
{
    options.ApiKey = builder.Configuration["ExternalServices:Stripe:PublishableKey"];
    options.SecretKey = builder.Configuration["ExternalServices:Stripe:SecretKey"];
});
```

### Strongly-Typed Configuration

```csharp
public class DatabaseOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}

// In Program.cs
builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection("ConnectionStrings"));
```

## Security Notes

✅ **This example demonstrates secure practices:**

- Secrets are never hardcoded in source code
- Connection strings and API keys are stored in 1Password
- Local development uses environment variables (not committed to git)
- Secret values are never logged or exposed via API endpoints

⚠️ **Important:**

- Never commit `appsettings.json` with real tokens to version control
- Use environment variables for `OnePassword:Token` in production
- The `/config/status` endpoint only shows whether secrets are configured, not their values

## Troubleshooting

### "OnePassword:Token not configured"

Set the token via environment variable:

```bash
export OnePassword__Token="your-token-here"
```

### "Authentication failed"

Check that:
- Your token is valid and not expired
- The Connect server URL is correct and accessible
- The Connect server is running

### "Vault not found" or "Item not found"

Verify that:
- The vault name in your `op://` URIs matches exactly
- The item name exists in that vault
- Your token has access to that vault

### Secrets not resolving

Check the application logs for:
```
OnePasswordConfigurationProvider loading...
Found X op:// URIs to resolve
Successfully resolved X of X secrets in Yms
```

If you see errors, the logs will indicate which secret failed to resolve.

## Production Deployment

### Using Environment Variables

Set all sensitive configuration via environment variables:

```bash
# 1Password credentials
OnePassword__ConnectServer=https://connect.example.com
OnePassword__Token=your-production-token

# Override specific secrets for this environment
ConnectionStrings__Database=your-production-connection-string
```

### Using Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY publish/ .

ENV OnePassword__ConnectServer=https://connect.example.com
ENV OnePassword__Token=${ONEPASSWORD_TOKEN}

ENTRYPOINT ["dotnet", "AspNetCoreIntegration.dll"]
```

### Using Kubernetes

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: app-config
data:
  OnePassword__ConnectServer: "https://connect.example.com"

---
apiVersion: v1
kind: Secret
metadata:
  name: app-secrets
type: Opaque
data:
  onepassword-token: <base64-encoded-token>

---
apiVersion: apps/v1
kind: Deployment
spec:
  template:
    spec:
      containers:
      - name: app
        env:
        - name: OnePassword__Token
          valueFrom:
            secretKeyRef:
              name: app-secrets
              key: onepassword-token
        envFrom:
        - configMapRef:
            name: app-config
```

## Next Steps

- Add Entity Framework and a real database
- Configure external services (Stripe, SendGrid, etc.)
- Add authentication and authorization
- Deploy to production (Azure, AWS, Kubernetes)

## Related Examples

- **[ProgrammaticAccess](../ProgrammaticAccess)** - Direct SDK usage without configuration provider
- **[ConfigurationIntegration](../ConfigurationIntegration)** - Console app with configuration integration

## Documentation

- [Quickstart Guide](../../specs/001-onepassword-sdk/quickstart.md)
- [API Reference](../../specs/001-onepassword-sdk/contracts/README.md)
- [1Password Connect](https://developer.1password.com/docs/connect/)
