# API Contracts

**Feature**: 001-onepassword-sdk
**Purpose**: Define public API contracts for the 1Password .NET SDK

## Overview

This directory contains the API contract specifications for the 1Password .NET SDK. These contracts define the public surface area that consumers will interact with.

## Contract Files

### Core SDK Contracts

#### `IOnePasswordClient.cs`
Primary client interface for programmatic 1Password access.

**User Stories**: Supports User Story 1 (Programmatic Vault Access)

**Key Operations**:
- Vault operations: `ListVaultsAsync`, `GetVaultAsync`, `GetVaultByTitleAsync`
- Item operations: `ListItemsAsync`, `GetItemAsync`, `GetItemByTitleAsync`
- Field operations: `GetSecretAsync`
- Batch operations: `GetSecretsAsync` (up to 100 secrets)

**Requirements Addressed**: FR-001 through FR-009, FR-018, FR-019, FR-021 through FR-034

---

### Configuration Provider Contracts

#### `IOnePasswordConfigurationSource.cs`
Integration with Microsoft.Extensions.Configuration system.

**User Stories**: Supports User Story 2 (Configuration Builder Integration)

**Key Components**:
- `OnePasswordConfigurationSource`: Configuration source implementation
- `IOnePasswordConfigurationProvider`: Provider that resolves op:// URIs (internal)

**Requirements Addressed**: FR-010 through FR-024

---

#### `ConfigurationBuilderExtensions.cs`
Fluent extension methods for easy integration.

**User Stories**: Supports User Story 2 (Configuration Builder Integration)

**API Variants**:
1. `AddOnePassword()`: Auto-discover credentials from configuration
2. `AddOnePassword(connectServer, token)`: Explicit credentials
3. `AddOnePassword(configureOptions)`: Advanced configuration

**Requirements Addressed**: FR-006, FR-007, SC-001 (< 10 lines of code)

---

### Exception Contracts

#### `Exceptions.cs`
Complete exception hierarchy for error handling.

**Requirements Addressed**: FR-025 through FR-034

**Exception Types**:
- `OnePasswordException`: Base exception
- `AuthenticationException`: Invalid/expired token (FR-021)
- `AccessDeniedException`: Permission denied (FR-031, FR-034)
- `VaultNotFoundException`: Vault not found
- `ItemNotFoundException`: Item not found
- `FieldNotFoundException`: Field not found (FR-026)
- `NetworkException`: Network failures after retries (FR-033)
- `MalformedUriException`: Invalid op:// URI (FR-026, FR-028, FR-029, FR-032)
- `BatchSizeExceededException`: >100 secrets (FR-022)
- `SecretSizeExceededException`: >1MB secret (FR-023)
- `BatchTimeoutException`: >10s timeout (FR-024)

---

## Design Principles

### 1. Constitution Alignment

All contracts align with the project constitution:

- **Security-First** (Principle I): No secret values in exception messages, HTTPS required
- **Library-First Architecture** (Principle II): Framework-agnostic, minimal dependencies
- **API Simplicity** (Principle III): Fluent APIs, pit of success design, helpful error messages
- **Observability** (Principle V): All operations support ILogger integration (implementation detail)

### 2. Async Throughout

All I/O operations are async per Constitution Principle II requirement for async API variants.

### 3. Cancellation Support

All async methods accept `CancellationToken` for graceful cancellation.

### 4. Fail-Fast Validation

- op:// URI syntax validated before API calls (FR-028, FR-031)
- Batch size limits checked before retrieval (FR-022)
- All errors fail configuration build immediately (FR-027, FR-030)

### 5. Context-Rich Errors

All exceptions include context per FR-026:
- Vault ID, Item ID, Field Label when applicable
- Configuration key for malformed URIs
- Actual vs. expected values for limit violations

---

## Usage Examples

### Example 1: Programmatic Secret Retrieval

```csharp
using OnePassword.Sdk;

var options = new OnePasswordClientOptions
{
    ConnectServer = "https://connect.example.com",
    Token = Environment.GetEnvironmentVariable("OP_TOKEN")
};

using var client = new OnePasswordClient(options);

// Get a specific secret
var password = await client.GetSecretAsync(
    vaultId: "production",
    itemId: "database",
    fieldLabel: "password");

// Batch retrieve multiple secrets
var secrets = await client.GetSecretsAsync(new[]
{
    "op://prod/database/password",
    "op://prod/api-keys/stripe/secret"
});
```

### Example 2: Configuration Integration

```csharp
using Microsoft.Extensions.Configuration;
using OnePassword.Configuration;

// appsettings.json:
// {
//   "ConnectionStrings": {
//     "Database": "op://prod/database/connection_string"
//   },
//   "OnePassword": {
//     "ConnectServer": "https://connect.example.com",
//     "Token": "your-token-here"
//   }
// }

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .AddOnePassword()  // Automatically resolves op:// URIs
    .Build();

// Reads actual secret value, not "op://..."
var connectionString = configuration["ConnectionStrings:Database"];
```

### Example 3: Error Handling

```csharp
using OnePassword.Sdk;
using OnePassword.Sdk.Exceptions;

try
{
    var secret = await client.GetSecretAsync("vault", "item", "field");
}
catch (FieldNotFoundException ex)
{
    // Field not found - check field name
    Console.WriteLine($"Field '{ex.FieldLabel}' not found in vault '{ex.VaultId}', item '{ex.ItemId}'");
}
catch (AuthenticationException ex)
{
    // Token invalid or expired - refresh token and restart
    Console.WriteLine("Authentication failed. Check your token.");
}
catch (NetworkException ex)
{
    // Network issue after retries - check connectivity
    Console.WriteLine($"Network error after {ex.RetryAttempts} retries");
}
catch (OnePasswordException ex)
{
    // Other 1Password errors
    Console.WriteLine($"1Password error: {ex.Message}");
}
```

---

## Contract Validation Checklist

Before implementation, verify contracts satisfy:

- ✅ All functional requirements (FR-001 through FR-044) represented
- ✅ All user stories supported (US1, US2, US3)
- ✅ All success criteria achievable (SC-001 through SC-008)
- ✅ Constitution principles honored (I through V)
- ✅ All edge cases covered (malformed URIs, limits, timeouts, permissions)
- ✅ Security requirements enforced (no secret logging, HTTPS, sanitization)
- ✅ Observability hooks available (ILogger integration)

---

## Next Steps

After contracts are approved:

1. **Implement Core SDK** (`OnePassword.Sdk` package)
   - `OnePasswordClient` implementing `IOnePasswordClient`
   - Exception classes from `Exceptions.cs`
   - Domain models (Vault, Item, Field, SecretReference)

2. **Implement Configuration Provider** (`OnePassword.Configuration` package)
   - `OnePasswordConfigurationProvider` implementing `IConfigurationProvider`
   - `OnePasswordConfigurationSource`
   - Extension methods from `ConfigurationBuilderExtensions`

3. **Write Tests**
   - Unit tests for URI parsing, validation, batch logic
   - Integration tests with mocked 1Password API
   - End-to-end tests for configuration scenarios

4. **Generate Documentation**
   - XML documentation comments (already in contracts)
   - README with quickstart guide
   - API reference documentation

---

**Status**: Contracts ready for Phase 2 (Task Generation)
**Last Updated**: 2025-11-18
