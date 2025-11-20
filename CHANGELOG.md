# Changelog

All notable changes to the 1Password .NET SDK will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0-beta1] - 2025-11-19

### Added

#### Core SDK (OnePassword.Sdk)
- **IOnePasswordClient** interface for 1Password Connect API access
- **OnePasswordClient** implementation with comprehensive error handling and retry logic
- **Vault Operations**:
  - `ListVaultsAsync()` - List all accessible vaults
  - `GetVaultAsync(id)` - Get vault by ID
  - `GetVaultByTitleAsync(title)` - Get vault by title
- **Item Operations**:
  - `ListItemsAsync(vaultId)` - List items in a vault
  - `GetItemAsync(vaultId, itemId)` - Get item by ID
  - `GetItemByTitleAsync(vaultId, title)` - Get item by title
- **Secret Operations**:
  - `GetSecretAsync(vaultId, itemId, fieldLabel)` - Get single secret value
  - `GetSecretsAsync(references)` - Batch retrieve up to 100 secrets
- **Exception Hierarchy**:
  - `OnePasswordException` - Base exception
  - `AuthenticationException` - Invalid/expired token
  - `AccessDeniedException` - Insufficient permissions
  - `VaultNotFoundException` - Vault not found
  - `ItemNotFoundException` - Item not found
  - `FieldNotFoundException` - Field not found
  - `NetworkException` - Network/connectivity errors
  - `MalformedUriException` - Invalid op:// URI format
  - `BatchSizeExceededException` - Too many secrets in batch
  - `SecretSizeExceededException` - Secret exceeds 1MB limit
  - `BatchTimeoutException` - Batch operation timeout
- **Domain Models**:
  - `Vault` - Vault metadata
  - `Item` - Item with fields and sections
  - `Field` - Field with secure `ToString()` (excludes Value)
  - `Section` - Item section grouping
  - `OnePasswordClientOptions` - Client configuration
- **Retry Policy**: Exponential backoff (1s, 2s, 4s) for transient failures
- **Logging**: Structured logging with correlation IDs (INFO, WARN, ERROR levels)
- **Security**: HTTPS enforcement, no secret logging, token sanitization

#### Configuration Provider (OnePassword.Configuration)
- **OnePasswordConfigurationProvider** - Automatic op:// URI resolution
- **ConfigurationBuilderExtensions** - Fluent API integration
  - `AddOnePassword()` - Read credentials from configuration
  - `AddOnePassword(server, token)` - Explicit credentials
  - `AddOnePassword(options => {...})` - Configuration action
- **Environment Variable Override**: Environment variables automatically override op:// URIs
- **Batch Resolution**: All secrets resolved in single API call at startup
- **In-Memory Caching**: Resolved secrets cached for application lifetime
- **Logging**: Load lifecycle events with timing metrics

### Features

- **Multi-Targeting**: .NET 6.0 and .NET 8.0 support
- **op:// URI Parsing**: Full support for `op://vault/item/field` and `op://vault/item/section/field`
- **Configuration Precedence**: Respects standard .NET configuration precedence (environment variables > secrets)
- **Fail-Fast**: Invalid configuration fails at startup (not runtime)
- **Zero Dependencies**: Core SDK has minimal dependencies (Microsoft.Extensions.Logging.Abstractions)
- **Dispose Pattern**: Proper resource cleanup with IDisposable
- **CancellationToken Support**: All async methods support cancellation
- **Correlation IDs**: Request tracing with System.Diagnostics.Activity

### Performance

- **Batch Operations**: Retrieve 100 secrets in single call (<10s)
- **Startup Overhead**: Configuration resolution <500ms for 20 secrets
- **Secret Size Limit**: 1MB per secret
- **Batch Timeout**: 10 seconds for batch operations
- **HTTP Timeout**: 10 seconds (configurable)

### Security

- **HTTPS Only**: HTTP URLs rejected at runtime
- **Token Protection**: Tokens never logged or exposed in error messages
- **Field Security**: `Field.ToString()` excludes Value property
- **Exception Sanitization**: No partial secrets in error messages
- **Least Privilege**: Service account permissions enforced by 1Password Connect

### Documentation

- Comprehensive **Quickstart Guide** with 3 use cases
- **API Reference** with detailed contracts
- **Configuration Guide** for ASP.NET Core integration
- **Environment Override Guide** for local development
- **Security Best Practices** guide
- **Troubleshooting** section with common issues

### Testing

- **69 passing unit tests**
  - 48 SDK tests (exceptions, models, client operations, batch operations)
  - 21 Configuration provider tests (builder extensions, precedence, validation)
- **Test Coverage**:
  - Exception handling
  - Validation logic
  - Configuration precedence
  - Batch operations
  - URI parsing
  - Argument validation

### Known Limitations

- **Beta Software**: API may change before 1.0.0 release
- **Connect API Only**: Requires 1Password Connect (not 1Password.com directly)
- **Read-Only**: No support for creating/updating items (Connect API limitation)
- **In-Memory Caching**: Secrets cached for application lifetime (no auto-refresh)
- **No Retry Configuration**: Retry policy is fixed (3 attempts with exponential backoff)

### Breaking Changes from Future 1.0.0

- This is the initial beta release
- No breaking changes yet

---

## [Unreleased]

### Planned for 1.0.0
- Integration tests with mock 1Password Connect server
- Performance benchmarks (BenchmarkDotNet)
- Example projects (console, ASP.NET Core)
- Code coverage reporting (>80% target)
- NuGet package publishing
- GitHub Actions CI/CD

---

[1.0.0-beta1]: https://github.com/maxisam/dotnet-1password/releases/tag/v1.0.0-beta1
[Unreleased]: https://github.com/maxisam/dotnet-1password/compare/v1.0.0-beta1...HEAD
