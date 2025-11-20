# Research: HttpClientFactory and Polly v8 Integration

**Feature**: 002-httpclient-factory-polly
**Date**: 2025-11-19
**Purpose**: Determine best practices for integrating IHttpClientFactory with Microsoft.Extensions.Http.Resilience (https://learn.microsoft.com/en-us/dotnet/core/resilience/?tabs=dotnet-cli) policies

## Research Questions

1. How to configure Microsoft.Extensions.Http.Resilience (https://learn.microsoft.com/en-us/dotnet/core/resilience/?tabs=dotnet-cli) policies with IHttpClientFactory?
2. What is the recommended pattern for jittered exponential backoff?
3. How to implement circuit breaker with proper state tracking per named client?
4. How to handle graceful shutdown with in-flight requests?
5. How to distinguish transient from permanent HTTP errors?

## Findings

### 1. IHttpClientFactory + Polly v8 Integration

**Decision**: Use `Microsoft.Extensions.Http.Resilience` package (Polly v8 native integration)

**Rationale**:
- Polly v8 introduced a new resilience pipeline API that replaces the old Policy<T> pattern
- `Microsoft.Extensions.Http.Resilience` provides `AddStandardResilienceHandler()` and `AddResilienceHandler()` extensions
- These extensions integrate directly with IHttpClientFactory's named/typed client registration
- Polly v8's resilience pipelines support telemetry, metrics, and structured logging out-of-the-box

**Alternatives Considered**:
- **Polly.Extensions.Http (v3.x)** - Rejected: Designed for Polly v7, uses deprecated Policy<T> API
- **Manual DelegatingHandler** - Rejected: Requires more boilerplate, misses built-in telemetry

**Implementation Pattern**:
```csharp
services.AddHttpClient("OnePasswordClient")
    .AddResilienceHandler("OnePasswordResilience", (builder, context) =>
    {
        builder
            .AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = options.MaxRetries,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true, // FR-015: Jittered exponential backoff
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>(ex => !ex.CancellationToken.IsCancellationRequested)
                    .HandleResult(response => IsTransientError(response.StatusCode))
            })
            .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(60),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30)
            });
    });
```

**Package Requirement**: Add `Microsoft.Extensions.Http.Resilience` (version 8.x) to OnePassword.Sdk.csproj

### 2. Jittered Exponential Backoff (FR-015)

**Decision**: Use Polly v8's built-in `UseJitter = true` with `BackoffType = DelayBackoffType.Exponential`

**Rationale**:
- Polly v8's HttpRetryStrategyOptions includes native jitter support
- Jitter prevents "thundering herd" when multiple clients retry simultaneously
- Formula: `delay = baseDelay * (2 ^ attemptNumber) + randomJitter`
- Random jitter typically adds ±25% variance to computed delay

**Alternatives Considered**:
- **Custom jitter implementation** - Rejected: Polly v8's jitter is battle-tested and integrates with telemetry
- **Fixed delays** - Rejected: Violates FR-015, causes thundering herd

**Configuration**:
```csharp
new HttpRetryStrategyOptions
{
    Delay = TimeSpan.FromSeconds(1),        // Base delay
    BackoffType = DelayBackoffType.Exponential, // 1s, 2s, 4s, 8s, ...
    UseJitter = true,                       // Adds ±25% randomness
    MaxDelay = TimeSpan.FromSeconds(30)     // Cap maximum delay
}
```

### 3. Circuit Breaker State Tracking (FR-013)

**Decision**: Use named HttpClient registration with scoped resilience handlers

**Rationale**:
- Each named HttpClient gets its own resilience pipeline instance
- Circuit breaker state is isolated per pipeline, not shared globally
- Consumers using DI will automatically get isolated circuit breakers per IOnePasswordClient instance
- Manual instantiation (new OnePasswordClient(options)) can optionally create a new IHttpClientFactory with its own state

**Alternatives Considered**:
- **Global circuit breaker** - Rejected: Violates FR-013, causes cross-client interference
- **Static policy** - Rejected: Not thread-safe for concurrent clients, state leaks across instances

**Implementation**:
```csharp
// Each AddHttpClient registration gets isolated state
services.AddHttpClient("OnePasswordClient-Instance1")
    .AddResilienceHandler("Resilience1", ...);

services.AddHttpClient("OnePasswordClient-Instance2")
    .AddResilienceHandler("Resilience2", ...);
```

**Circuit Breaker Configuration (FR-014)**:
```csharp
new HttpCircuitBreakerStrategyOptions
{
    FailureRatio = 0.5,                     // Open after 50% failures
    SamplingDuration = TimeSpan.FromSeconds(60), // Track failures over 60s (FR-014 default)
    MinimumThroughput = 5,                  // Require 5 calls before opening (FR-014 default threshold)
    BreakDuration = TimeSpan.FromSeconds(30) // FR-014 default break duration
}
```

### 4. Graceful Shutdown (FR-011)

**Decision**: Rely on IHttpClientFactory's built-in handler lifetime management + custom disposal logic

**Rationale**:
- IHttpClientFactory manages HttpMessageHandler lifetime (default: 2 minutes)
- Handlers are pooled and reused, disposing them gracefully when no longer referenced
- For in-flight requests, we need custom logic in OnePasswordClient.Dispose():
  1. Set a disposed flag to reject new requests
  2. Wait for in-flight requests with timeout (grace period)
  3. Cancel remaining requests after grace period

**Alternatives Considered**:
- **Immediate cancellation** - Rejected: Violates FR-011 requirement for graceful completion
- **Indefinite wait** - Rejected: Can cause shutdown hangs

**Implementation Approach**:
```csharp
private readonly SemaphoreSlim _requestSemaphore = new(int.MaxValue);
private bool _disposed = false;

public async Task<T> SendRequestAsync<T>(...)
{
    if (_disposed) throw new ObjectDisposedException(nameof(OnePasswordClient));

    await _requestSemaphore.WaitAsync(cancellationToken);
    try
    {
        // Make HTTP request
    }
    finally
    {
        _requestSemaphore.Release();
    }
}

public void Dispose()
{
    if (_disposed) return;
    _disposed = true;

    // Wait for in-flight requests (grace period: 5 seconds)
    _requestSemaphore.Wait(TimeSpan.FromSeconds(5));
    _requestSemaphore.Dispose();
}
```

### 5. Transient vs Permanent Error Detection (FR-005, FR-010)

**Decision**: Implement TransientErrorDetector with predicate-based classification

**Rationale**:
- Polly v8's `ShouldHandle` predicate allows fine-grained error classification
- Transient errors: 408 (Timeout), 429 (Too Many Requests), 500 (Internal Server Error), 502 (Bad Gateway), 503 (Service Unavailable), 504 (Gateway Timeout)
- Permanent errors (non-retryable): 401 (Unauthorized), 403 (Forbidden), 404 (Not Found) - FR-010

**Alternatives Considered**:
- **Retry all 4xx/5xx** - Rejected: Violates FR-010, wastes resources retrying auth failures
- **Polly.Extensions.Http.HttpPolicyExtensions.HandleTransientHttpError()** - Rejected: Polly v7 API, doesn't cover 408/429

**Classification Logic**:
```csharp
public static bool IsTransientError(HttpStatusCode statusCode)
{
    return statusCode switch
    {
        HttpStatusCode.RequestTimeout => true,              // 408
        (HttpStatusCode)429 => true,                        // Too Many Requests
        HttpStatusCode.InternalServerError => true,         // 500
        HttpStatusCode.BadGateway => true,                  // 502
        HttpStatusCode.ServiceUnavailable => true,          // 503
        HttpStatusCode.GatewayTimeout => true,              // 504
        _ => false
    };
}

public static bool IsPermanentError(HttpStatusCode statusCode)
{
    return statusCode is
        HttpStatusCode.Unauthorized or              // 401 - FR-010
        HttpStatusCode.Forbidden or                 // 403 - FR-010
        HttpStatusCode.NotFound;                    // 404 - FR-010
}
```

### 6. Timeout Handling (FR-017)

**Decision**: Use Polly v8's timeout strategy combined with overall request timeout

**Rationale**:
- Polly pipelines execute strategies in sequence: timeout → retry → circuit breaker
- Per-attempt timeout ensures individual requests don't hang
- Overall timeout tracked in SendRequestAsync to implement FR-017 (skip retries if budget exceeded)

**Implementation**:
```csharp
builder
    .AddTimeout(new HttpTimeoutStrategyOptions
    {
        Timeout = TimeSpan.FromSeconds(10) // Per-attempt timeout
    })
    .AddRetry(...) // Retry with backoff
    .AddCircuitBreaker(...);

// In SendRequestAsync: track overall budget
var overallTimeout = DateTime.UtcNow.Add(_options.Timeout);
// Polly will respect this through CancellationToken
```

### 7. Batch Operation Resilience (FR-016)

**Decision**: Wrap batch operations with try-catch for BrokenCircuitException

**Rationale**:
- GetSecretsAsync fetches multiple items in parallel
- If circuit opens mid-batch, Polly throws BrokenCircuitException
- Catch this exception, return partial results, include circuit state in exception details

**Implementation**:
```csharp
var results = new Dictionary<string, string>();
var failures = new List<string>();

await Parallel.ForEachAsync(references, async (ref, ct) =>
{
    try
    {
        var value = await GetSecretAsync(ref.vault, ref.item, ref.field, ct);
        results[ref.originalUri] = value;
    }
    catch (BrokenCircuitException ex)
    {
        failures.Add(ref.originalUri);
    }
});

if (failures.Any())
{
    throw new CircuitBreakerOpenException(
        $"Circuit breaker opened during batch operation. Successfully fetched {results.Count}, failed: {failures.Count}",
        results,
        failures
    );
}
```

## Configuration Validation (FR-018)

**Decision**: Validate OnePasswordClientOptions in constructor before creating HttpClient

**Rationale**:
- Fail-fast validation prevents runtime errors
- Clear ArgumentException messages guide developers to fix configuration

**Validation Rules**:
```csharp
public void Validate()
{
    // Existing validations
    if (string.IsNullOrWhiteSpace(Token))
        throw new ArgumentException("Token cannot be null or empty", nameof(Token));

    if (!ConnectServer.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("ConnectServer must use HTTPS", nameof(ConnectServer));

    // New validations for FR-018
    if (MaxRetries < 0)
        throw new ArgumentException("MaxRetries cannot be negative", nameof(MaxRetries));

    if (Timeout <= TimeSpan.Zero)
        throw new ArgumentException("Timeout must be greater than zero", nameof(Timeout));

    if (CircuitBreakerFailureThreshold < 1)
        throw new ArgumentException("CircuitBreakerFailureThreshold must be at least 1", nameof(CircuitBreakerFailureThreshold));

    if (CircuitBreakerBreakDuration <= TimeSpan.Zero)
        throw new ArgumentException("CircuitBreakerBreakDuration must be greater than zero", nameof(CircuitBreakerBreakDuration));
}
```

## Dependencies Summary

**New Package Requirements**:
1. `Microsoft.Extensions.Http.Resilience` (version 8.x) - Polly v8 integration with IHttpClientFactory
2. `Microsoft.Extensions.DependencyInjection.Abstractions` (version 8.x) - For IServiceCollection extensions

**Existing Packages**:
- Polly 8.4.2 - Already in csproj, used by Microsoft.Extensions.Http.Resilience
- Microsoft.Extensions.Logging.Abstractions 8.0.0 - Already in csproj, used for logging

## References

- [Polly v8 Migration Guide](https://www.thepollyproject.org/2024/02/12/polly-v8-release/)
- [Microsoft.Extensions.Http.Resilience Documentation](https://learn.microsoft.com/en-us/dotnet/core/resilience/)
- [IHttpClientFactory Best Practices](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests)
- [Polly v8 Resilience Pipelines](https://github.com/App-vNext/Polly#resilience-pipelines)
