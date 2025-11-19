# Research: 1Password .NET SDK

**Feature**: 001-onepassword-sdk
**Date**: 2025-11-18
**Purpose**: Resolve NEEDS CLARIFICATION items from Technical Context and establish best practices for implementation

## Research Questions

### 1. JSON Serialization Library (System.Text.Json vs Newtonsoft.Json)

**Decision**: Use **System.Text.Json**

**Rationale**:
- **Built-in to .NET**: No additional dependency required (System.Text.Json ships with .NET)
- **Performance**: Significantly faster serialization/deserialization than Newtonsoft.Json
- **Modern API**: Better async support, source generators for AOT scenarios
- **Security**: Active maintenance and security updates from Microsoft
- **Alignment with .NET ecosystem**: Microsoft.Extensions.* libraries use System.Text.Json
- **Constitution compliance**: Minimizes external dependencies (Library-First Architecture principle II)

**Alternatives Considered**:
- **Newtonsoft.Json**: Rejected because it's an additional dependency, slower performance, and not the modern .NET standard
- **Custom JSON parser**: Rejected as over-engineering; no unique requirements justify this complexity

**References**:
- [System.Text.Json overview](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/overview)
- [Performance comparison](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/migrate-from-newtonsoft)

---

### 2. HTTP Client Resilience Patterns (Polly vs Manual Implementation)

**Decision**: Use **Polly** for retry/timeout policies

**Rationale**:
- **Industry standard**: Polly is the de facto resilience library for .NET HTTP clients
- **Well-tested**: Mature library (10+ years) with comprehensive test coverage
- **Rich features**: Provides retry with exponential backoff, circuit breakers, timeout policies out-of-the-box
- **HttpClientFactory integration**: Native integration with IHttpClientFactory for proper HttpClient lifecycle management
- **Simplified implementation**: Spec requires retry (3x with exponential backoff) and timeout (10s) - Polly handles this declaratively
- **Security**: Active maintenance, security patches, and community review
- **Constitution compliance**: While it adds a dependency, it's justified by security and reliability requirements (Principle I, V)

**Implementation approach**:
```csharp
services.AddHttpClient<IOnePasswordClient, OnePasswordClient>()
    .AddPolicyHandler(Policy
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1))))
    .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10)));
```

**Alternatives Considered**:
- **Manual retry implementation**: Rejected because it duplicates well-tested logic, increases bug surface area, and violates DRY principle
- **Built-in HttpClient timeout only**: Rejected because it doesn't provide exponential backoff retry logic required by spec (FR-033)
- **No resilience library**: Rejected due to security and reliability requirements in constitution (Principle I)

**References**:
- [Polly documentation](https://github.com/App-vNext/Polly)
- [Microsoft IHttpClientFactory with Polly](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/implement-http-call-retries-exponential-backoff-polly)

---

### 3. Testing Framework (xUnit vs NUnit)

**Decision**: Use **xUnit**

**Rationale**:
- **Modern design**: xUnit is the modern, recommended testing framework for .NET
- **Better isolation**: Each test runs in its own instance (no shared state between tests)
- **Async-first**: Native support for async test methods
- **Extensibility**: Rich ecosystem of extensions and assertion libraries
- **Microsoft preference**: Used by Microsoft in ASP.NET Core, Entity Framework Core, and other official projects
- **Community adoption**: Most popular testing framework in modern .NET projects
- **Constitution alignment**: Testing Standards require independent tests (xUnit's design supports this by default)

**Assertion Library**: Use **FluentAssertions** for readable test assertions

**Mocking Library**: Use **Moq** for mocking (industry standard, well-documented)

**Alternatives Considered**:
- **NUnit**: Rejected because it uses shared test context (can lead to state pollution), less modern API design
- **MSTest**: Rejected because it's less feature-rich and has smaller community adoption

**References**:
- [xUnit documentation](https://xunit.net/)
- [xUnit comparisons](https://xunit.net/docs/comparisons)

---

### 4. 1Password Connect API Structure and Patterns

**Research**: Analysis of official 1Password SDKs (JavaScript, Python) to ensure .NET SDK follows established patterns

**Key Findings from JavaScript SDK** ([connect-sdk-js](https://github.com/1Password/connect-sdk-js)):
- **Client structure**: Single `OnePasswordConnect` client class with methods for vaults, items, files
- **API methods**:
  - `listVaults()`, `getVault(id)`, `getVaultsByTitle(title)`
  - `listItems(vaultId)`, `getItem(vaultId, itemId)`, `getItemByTitle(vaultId, title)`
  - Async/await pattern throughout
- **Error handling**: Custom error classes extending base Error
- **Configuration**: Client initialized with server URL and token

**Key Findings from Python SDK** ([connect-sdk-python](https://github.com/1Password/connect-sdk-python)):
- **Client structure**: `OnePasswordConnect` class with similar method naming
- **Authentication**: Token passed to client constructor
- **Models**: Vault, Item, Field classes with data attributes
- **Response handling**: Returns typed objects, not raw JSON

**Decision**: Port the client structure from JavaScript/Python SDKs to .NET

**API Design** (aligned with official SDKs):
```csharp
public interface IOnePasswordClient
{
    // Vault operations
    Task<IEnumerable<Vault>> ListVaultsAsync(CancellationToken cancellationToken = default);
    Task<Vault> GetVaultAsync(string vaultId, CancellationToken cancellationToken = default);
    Task<Vault> GetVaultByTitleAsync(string title, CancellationToken cancellationToken = default);

    // Item operations
    Task<IEnumerable<Item>> ListItemsAsync(string vaultId, CancellationToken cancellationToken = default);
    Task<Item> GetItemAsync(string vaultId, string itemId, CancellationToken cancellationToken = default);
    Task<Item> GetItemByTitleAsync(string vaultId, string title, CancellationToken cancellationToken = default);

    // Field extraction (convenience method)
    Task<string> GetSecretAsync(string vaultId, string itemId, string fieldName, CancellationToken cancellationToken = default);
}
```

**Rationale**:
- Follows established 1Password SDK conventions (per Constitution Principle II)
- Async throughout (required by Constitution for I/O operations)
- CancellationToken support for graceful cancellation
- Strongly typed return values (Vault, Item) rather than dynamic/JSON

---

### 5. op:// URI Parsing Strategy

**Research**: Official 1Password secret reference syntax

**Official Format** (from [1Password docs](https://developer.1password.com/docs/cli/secret-reference-syntax/)):
```
op://<vault>/<item>/<field>
op://<vault>/<item>/<section>/<field>
```

**Decision**: Implement regex-based URI parser with strict validation

**Implementation approach**:
```csharp
public class SecretReference
{
    public string Vault { get; }
    public string Item { get; }
    public string Section { get; }  // Optional
    public string Field { get; }

    public static bool TryParse(string uri, out SecretReference reference, out string errorMessage);
}
```

**Validation rules**:
- Must start with `op://`
- Vault, item, field are required
- Section is optional
- URL encoding support for special characters (e.g., `op://vault/my%20item/password`)
- Fail fast on malformed URIs per spec (FR-028, FR-031, FR-032)

**Rationale**:
- Explicit validation prevents ambiguous error messages
- Type-safe parsing reduces runtime errors
- Aligns with spec requirement for immediate validation (before API calls)

---

### 6. Secret Caching Strategy for Configuration Provider

**Decision**: Use **ConcurrentDictionary** for in-memory secret cache

**Implementation approach**:
```csharp
private readonly ConcurrentDictionary<string, string> _secretCache = new();

public void Load()
{
    // 1. Scan all configuration keys for op:// URIs
    // 2. Batch retrieve all secrets from 1Password
    // 3. Cache resolved values in _secretCache
    // 4. Replace op:// URIs with cached values
}

public override string Get(string key)
{
    // Return cached secret if key was resolved from op://
    if (_secretCache.TryGetValue(key, out var secret))
        return secret;

    // Otherwise return original value
    return base.Get(key);
}
```

**Rationale**:
- **Thread-safe**: ConcurrentDictionary handles concurrent reads safely
- **Immutable after Load()**: Secrets are cached once at startup (per spec requirement FR-020)
- **Memory-efficient**: Only resolved secrets are cached (not all configuration)
- **Security**: No disk persistence, only in-memory (per FR-035)

**Alternatives Considered**:
- **Dictionary with lock**: Rejected because ConcurrentDictionary is more performant for read-heavy scenarios
- **IMemoryCache**: Rejected because it's over-engineering; we don't need expiration/eviction policies

---

### 7. Batch Secret Retrieval Implementation

**Decision**: Custom batching logic using 1Password Connect API

**Implementation approach**:
1. Configuration provider scans all keys/values during `Load()`
2. Collect all `op://` URIs into a list
3. Deduplicate URIs (FR-019)
4. Parse each URI to extract vault/item/field references
5. Group by vault and item to minimize API calls
6. Execute batch API calls: `GET /v1/vaults/{vault}/items/{item}` for each unique item
7. Extract requested fields from returned items
8. Cache all resolved values before returning

**Optimization**: Single HTTP request per unique vault+item combination (not per field)

**Rationale**:
- 1Password Connect API doesn't have a native "batch get secrets" endpoint
- Grouping by vault+item minimizes total HTTP requests
- Aligns with spec requirement for batch retrieval (FR-018)
- Meets <500ms startup goal for 20 secrets (each item fetch is <50ms typically)

**Error handling**: Any failed item fetch fails the entire batch (fail-fast per FR-027, FR-030)

---

## Best Practices Summary

### Dependency Choices
- **JSON**: System.Text.Json (built-in, fast, modern)
- **HTTP Resilience**: Polly (industry standard, well-tested)
- **Testing**: xUnit + FluentAssertions + Moq (modern, Microsoft-aligned)

### Architecture Patterns
- **Client Interface**: Follow official 1Password SDK patterns (JavaScript/Python)
- **Async/Await**: Throughout all I/O operations
- **Error Handling**: Custom exception hierarchy with context
- **Caching**: ConcurrentDictionary for thread-safe in-memory cache
- **Batch Retrieval**: Group by vault+item, single API call per unique item

### Security Practices
- No plaintext secret logging or persistence
- HTTPS only for API calls
- Input validation before API calls (op:// URI syntax)
- Error message sanitization (no partial secret values)

### Performance Targets
- <500ms startup overhead for 20 secrets
- <10s batch retrieval for 100 secrets
- Exponential backoff retry (1s, 2s, 4s)
- 10s timeout per batch operation

---

## Resolved Clarifications

| Original Question | Resolution |
|------------------|------------|
| JSON serialization library | System.Text.Json |
| HTTP client resilience | Polly for retry/timeout policies |
| Testing framework | xUnit with FluentAssertions and Moq |

All NEEDS CLARIFICATION items from Technical Context are now resolved. Ready to proceed to Phase 1: Design & Contracts.
