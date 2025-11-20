# Example: Programmatic Access

This example demonstrates how to use the 1Password .NET SDK to programmatically access vaults, items, and secrets.

## What This Example Shows

1. **List all vaults** accessible to your service account
2. **Get vault details** by ID or title
3. **List items** in a vault
4. **Get item details** including all fields
5. **Retrieve secret values** from concealed fields
6. **Batch retrieve secrets** using `op://` URIs

## Prerequisites

- .NET 8.0 SDK
- 1Password Connect server running (locally or remote)
- Valid access token for 1Password Connect

## Configuration

Set the following environment variables:

```bash
export ONEPASSWORD_CONNECT_SERVER="https://localhost:8080"
export ONEPASSWORD_TOKEN="your-access-token-here"
```

**Security Note**: Never hardcode tokens in source code or commit them to version control!

## Running the Example

From the `examples/ProgrammaticAccess/ProgrammaticAccess` directory:

```bash
# Build the project
dotnet build

# Run the example
dotnet run
```

## Expected Output

```
1Password .NET SDK - Programmatic Access Example
=================================================

Connecting to: https://localhost:8080
Note: Token is read from ONEPASSWORD_TOKEN environment variable

Example 1: Listing all vaults
------------------------------
  - Private (ID: abc123)
  - Work (ID: def456)

Example 2: Getting vault 'Private' by ID
------------------------------
  Vault: Private
  ID: abc123
  Description: Personal vault

Example 3: Listing items in vault 'Private'
------------------------------
  Found 15 items:
    - Email Account (Category: Login)
    - Bank Account (Category: Login)
    - WiFi Password (Category: Password)
    ... and 12 more

Example 4: Getting item 'Email Account'
------------------------------
  Title: Email Account
  Category: Login
  Fields:
    - username: STRING
      Value: user@example.com
    - password: CONCEALED
      Value: *** (concealed field, not displayed)
    - website: URL
      Value: https://mail.example.com

Example 5: Retrieving a secret field
------------------------------
  Field: password
  Type: CONCEALED
  Value: *** (not displayed for security)
  Note: In your app, you would use this value to connect to services

Example 6: Batch retrieve secrets
------------------------------
  Retrieving 3 secrets in batch:
    - op://Private/Email Account/username
    - op://Private/Bank Account/username
    - op://Private/WiFi Password/password

  Retrieved 3 secrets successfully
  Note: Secret values are not displayed for security

✅ All examples completed successfully!
```

## Error Handling

The example demonstrates comprehensive error handling:

- **AuthenticationException**: Invalid or expired token
- **VaultNotFoundException**: Vault doesn't exist or no permission
- **ItemNotFoundException**: Item doesn't exist in vault
- **FieldNotFoundException**: Field doesn't exist in item
- **NetworkException**: Connection issues (with retry count)
- **OnePasswordException**: General 1Password errors

## Code Highlights

### Creating the Client

```csharp
var options = new OnePasswordClientOptions
{
    ConnectServer = "https://localhost:8080",
    Token = "your-token",
    Timeout = TimeSpan.FromSeconds(10),
    MaxRetries = 3
};

using var client = new OnePasswordClient(options);
```

### Listing Vaults

```csharp
var vaults = await client.ListVaultsAsync();
foreach (var vault in vaults)
{
    Console.WriteLine($"{vault.Title} (ID: {vault.Id})");
}
```

### Getting a Specific Secret

```csharp
var password = await client.GetSecretAsync(
    vaultId: "production",
    itemId: "database",
    fieldLabel: "password");
```

### Batch Retrieving Secrets

```csharp
var secrets = await client.GetSecretsAsync(new[]
{
    "op://prod/db/password",
    "op://prod/api-keys/stripe",
    "op://prod/api-keys/sendgrid"
});

foreach (var (uri, value) in secrets)
{
    // Use the secret value
}
```

## Next Steps

- See [ConfigurationIntegration](../ConfigurationIntegration/) for automatic secret resolution in `appsettings.json`
- See [AspNetCoreIntegration](../AspNetCoreIntegration/) for ASP.NET Core usage
- Read the [Quickstart Guide](../../specs/001-onepassword-sdk/quickstart.md) for more examples

## Security Considerations

1. **Never log secret values** - The SDK automatically prevents this with `Field.ToString()`
2. **Store tokens securely** - Use environment variables, not configuration files
3. **Use least privilege** - Grant service accounts only necessary vault permissions
4. **Rotate tokens regularly** - Tokens don't auto-refresh, so rotate them periodically
5. **Use HTTPS only** - The SDK enforces HTTPS for Connect server URLs
