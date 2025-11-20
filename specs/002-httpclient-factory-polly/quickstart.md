# Quickstart: HttpClientFactory and Polly Resilience

**Feature**: 002-httpclient-factory-polly
**Audience**: Developers using the OnePassword SDK
**Purpose**: Migration guide and usage examples for the new resilience features

## What's New

This release refactors the OnePassword SDK to use `IHttpClientFactory` with Polly v8 resilience policies. Benefits include:

- **Automatic Retry**: Transient failures (503, timeouts, network errors) are automatically retried with exponential backoff
- **Circuit Breaker**: Prevents cascading failures by failing fast when the API is consistently unavailable
- **Connection Pooling**: Reduces overhead by reusing HTTP connections (50%+ improvement)
- **Dependency Injection Support**: Easily register the client in ASP.NET Core and other DI containers
- **Better Observability**: Structured logging for retry attempts and circuit breaker state changes

## Backward Compatibility

✅ **No breaking changes** - Existing code continues to work without modifications.

The public API (`IOnePasswordClient`, `OnePasswordClient`) remains unchanged. The resilience features work transparently behind the scenes.

```csharp
// Existing code works exactly as before
var options = new OnePasswordClientOptions
{
    ConnectServer = "https://connect.1password.com",
    Token = "your-token-here"
};

var client = new OnePasswordClient(options);
var vaults = await client.ListVaultsAsync();
```

## New Features

### 1. Configurable Resilience Policies

You can now fine-tune retry and circuit breaker behavior:

```csharp
var options = new OnePasswordClientOptions
{
    ConnectServer = "https://connect.1password.com",
    Token = "your-token-here",

    // Retry configuration
    MaxRetries = 5,                     // Retry up to 5 times (default: 3)
    RetryBaseDelay = TimeSpan.FromMilliseconds(500),  // Start with 500ms (default: 1s)
    RetryMaxDelay = TimeSpan.FromSeconds(30),         // Cap at 30s (default: 30s)
    EnableJitter = true,                // Add randomness to prevent thundering herd (default: true)

    // Circuit breaker configuration
    CircuitBreakerFailureThreshold = 3,  // Open after 3 consecutive failures (default: 5)
    CircuitBreakerBreakDuration = TimeSpan.FromSeconds(20),  // Stay open for 20s (default: 30s)
    CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(60), // Track failures over 60s window (default: 60s)

    // Timeout
    Timeout = TimeSpan.FromSeconds(60)   // Overall request timeout (default: 30s)
};

var client = new OnePasswordClient(options);
```

### 2. Dependency Injection Support

**Recommended for ASP.NET Core and DI-enabled applications:**

```csharp
// In Program.cs or Startup.cs
services.AddOnePasswordClient(options =>
{
    options.ConnectServer = configuration["OnePassword:ConnectServer"];
    options.Token = configuration["OnePassword:Token"];
    options.MaxRetries = 5;
    options.CircuitBreakerFailureThreshold = 3;
});

// Inject IOnePasswordClient into your services
public class SecretService
{
    private readonly IOnePasswordClient _onePasswordClient;

    public SecretService(IOnePasswordClient onePasswordClient)
    {
        _onePasswordClient = onePasswordClient;
    }

    public async Task<string> GetDatabasePasswordAsync()
    {
        return await _onePasswordClient.GetSecretAsync(
            vaultId: "my-vault",
            itemId: "database-credentials",
            fieldLabel: "password"
        );
    }
}
```

### 3. Structured Logging

Resilience events are automatically logged at appropriate levels:

**Warning Level** (actionable events):
- Retry attempts: `"Retry attempt {RetryCount} of {MaxRetries} after {Delay}ms"`
- Circuit breaker opens: `"Circuit breaker opened after {FailureCount} consecutive failures"`
- Circuit breaker transitions: `"Circuit breaker transitioned to {State} state"`

**Information Level** (operational visibility):
- Successful operations: `"Listed {Count} vaults successfully"`
- Circuit breaker closes: `"Circuit breaker closed after successful test request"`

**Debug Level** (troubleshooting):
- Policy execution details: `"Executing retry policy with delay {Delay}ms"`
- Jitter calculations: `"Applied jitter: base {BaseDelay}ms, actual {ActualDelay}ms"`

```csharp
// Configure logging in your application
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Warning); // See retry attempts and circuit breaker events
});
```

## How Resilience Works

### Retry with Exponential Backoff

When the 1Password API returns a transient error (503, timeout, network issue), the SDK automatically retries with increasing delays:

| Attempt | Base Delay | With Jitter (±25%) |
|---------|------------|---------------------|
| 1       | 1s         | 0.75s - 1.25s       |
| 2       | 2s         | 1.5s - 2.5s         |
| 3       | 4s         | 3s - 5s             |
| 4       | 8s         | 6s - 10s            |
| 5       | 16s        | 12s - 20s           |

**Non-retryable errors** (fail immediately):
- 401 Unauthorized (bad token)
- 403 Forbidden (insufficient permissions)
- 404 Not Found (vault/item doesn't exist)

### Circuit Breaker

The circuit breaker protects your application from wasting resources when the API is down:

**States**:
1. **Closed** (normal operation): Requests flow through normally
2. **Open** (API is down): Requests fail immediately without hitting the API
3. **Half-Open** (testing recovery): Allow one test request to check if API recovered

**Example Timeline**:
```text
T=0s:   Circuit is Closed, requests succeed
T=10s:  API starts failing, 3 consecutive failures occur
T=11s:  Circuit opens (threshold reached)
T=11s - T=41s: All requests fail immediately with CircuitBreakerOpenException
T=41s:  Circuit transitions to Half-Open, allows one test request
T=42s:  Test request succeeds, circuit closes
T=43s:  Normal operation resumes
```

### Batch Operation Resilience

When using `GetSecretsAsync`, the circuit breaker may open mid-batch. The SDK returns partial results:

```csharp
var references = new[]
{
    "op://vault1/item1/password",
    "op://vault1/item2/api-key",
    "op://vault2/item3/token",      // Circuit opens here
    "op://vault2/item4/secret"      // Not fetched
};

try
{
    var secrets = await client.GetSecretsAsync(references);
}
catch (CircuitBreakerOpenException ex)
{
    // Use partial results
    Console.WriteLine($"Successfully fetched: {ex.PartialResults.Count}");
    Console.WriteLine($"Failed due to circuit: {ex.FailedReferences.Count}");

    // Access partial results
    foreach (var (uri, value) in ex.PartialResults)
    {
        Console.WriteLine($"✓ {uri}: {value}");
    }

    // Log failed references
    foreach (var uri in ex.FailedReferences)
    {
        Console.WriteLine($"✗ {uri}: Circuit open");
    }
}
```

## Configuration Validation

Invalid configuration is detected at client initialization (fail-fast):

```csharp
var options = new OnePasswordClientOptions
{
    ConnectServer = "https://connect.1password.com",
    Token = "your-token",
    MaxRetries = -1,  // ❌ Invalid!
    Timeout = TimeSpan.Zero  // ❌ Invalid!
};

try
{
    var client = new OnePasswordClient(options);
}
catch (ArgumentException ex)
{
    Console.WriteLine(ex.Message);
    // "MaxRetries cannot be negative (Parameter 'MaxRetries')"
}
```

**Validation Rules**:
- `Token`: Cannot be null or empty
- `ConnectServer`: Must start with `https://`
- `Timeout`: Must be greater than zero
- `MaxRetries`: Cannot be negative
- `CircuitBreakerFailureThreshold`: Must be at least 1
- `CircuitBreakerBreakDuration`: Must be greater than zero
- `RetryBaseDelay`: Must be greater than zero
- `RetryMaxDelay`: Must be >= `RetryBaseDelay`

## Migration Checklist

For existing users upgrading to this version:

- [ ] **No code changes required** - Existing code continues to work
- [ ] **Optional**: Add logging configuration to observe retry attempts and circuit breaker events
- [ ] **Optional**: Fine-tune resilience settings for your environment (dev vs production)
- [ ] **Optional**: Migrate to dependency injection using `AddOnePasswordClient()`
- [ ] **Recommended**: Test batch operations with circuit breaker scenarios
- [ ] **Recommended**: Review logs after deployment to tune `MaxRetries` and circuit breaker thresholds

## Troubleshooting

### Too Many Retries

**Symptom**: Requests take too long when API is down

**Solution**: Reduce `MaxRetries` or `RetryMaxDelay`:
```csharp
options.MaxRetries = 2;  // Fail faster
options.RetryMaxDelay = TimeSpan.FromSeconds(10);  // Cap delays
```

### Circuit Breaker Opens Too Quickly

**Symptom**: Circuit opens after just a few failures

**Solution**: Increase `CircuitBreakerFailureThreshold`:
```csharp
options.CircuitBreakerFailureThreshold = 10;  // Tolerate more failures
```

### Circuit Stays Open Too Long

**Symptom**: Slow recovery after API comes back online

**Solution**: Reduce `CircuitBreakerBreakDuration`:
```csharp
options.CircuitBreakerBreakDuration = TimeSpan.FromSeconds(10);  // Test recovery sooner
```

### Not Enough Logging

**Solution**: Lower log level to Debug for detailed policy execution:
```csharp
services.AddLogging(builder =>
{
    builder.SetMinimumLevel(LogLevel.Debug);
});
```

## Best Practices

1. **Use Dependency Injection** - Allows proper HttpClient lifecycle management and avoids socket exhaustion
2. **Tune for Your Environment** - Development: fast failures, Production: more retries
3. **Monitor Circuit Breaker Events** - Warning-level logs indicate API health issues
4. **Handle Partial Results** - Batch operations may return partial results when circuit opens
5. **Set Realistic Timeouts** - Balance between user experience and retry budget

## Next Steps

- Read the [Architecture Documentation](data-model.md) for implementation details
- Review [Test Scenarios](../plan.md#project-structure) for comprehensive testing guidance
- Check [Polly Documentation](https://www.thepollyproject.org/) for advanced resilience patterns
