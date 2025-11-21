# Data Model: Resilience Configuration

**Feature**: 002-httpclient-factory-polly
**Date**: 2025-11-19
**Purpose**: Define configuration entities for HTTP resilience policies

## Overview

This refactoring introduces resilience configuration as an extension to the existing `OnePasswordClientOptions` class. No new data persistence is required - all configuration is in-memory and scoped to the client instance lifecycle.

## Entities

### OnePasswordClientOptions (Extended)

**Purpose**: Configuration object for OnePasswordClient with added resilience policy settings

**Existing Properties** (preserved for backward compatibility):
- `ConnectServer` (string): 1Password Connect server URL (must be HTTPS)
- `Token` (string): Authentication bearer token
- `Timeout` (TimeSpan): Overall request timeout (default: 30 seconds)
- `MaxRetries` (int): Maximum retry attempts (default: 3) - **Now controls retry policy**

**New Properties** (added for FR-007, FR-014):

| Property | Type | Default | Validation | Purpose |
|----------|------|---------|------------|---------|
| `CircuitBreakerFailureThreshold` | int | 5 | ≥ 1 | Consecutive failures before circuit opens (FR-014) |
| `CircuitBreakerBreakDuration` | TimeSpan | 30 seconds | > 0 | How long circuit stays open (FR-014) |
| `CircuitBreakerSamplingDuration` | TimeSpan | 60 seconds | > 0 | Window for tracking failure ratio (FR-014) |
| `RetryBaseDelay` | TimeSpan | 1 second | > 0 | Base delay for exponential backoff (FR-003, FR-015) |
| `RetryMaxDelay` | TimeSpan | 30 seconds | > 0 | Maximum delay cap for backoff |
| `EnableJitter` | bool | true | N/A | Enable jittered backoff (FR-015) |

**Relationships**:
- Used by `ResiliencePolicyBuilder` to create Polly policies
- Validated by `OnePasswordClientOptions.Validate()` method (FR-018)

**State Transitions**: None (immutable configuration)

**Validation Rules** (FR-018):
```csharp
- Token: MUST NOT be null or whitespace
- ConnectServer: MUST start with "https://"
- Timeout: MUST be > TimeSpan.Zero
- MaxRetries: MUST be ≥ 0
- CircuitBreakerFailureThreshold: MUST be ≥ 1
- CircuitBreakerBreakDuration: MUST be > TimeSpan.Zero
- CircuitBreakerSamplingDuration: MUST be > TimeSpan.Zero
- RetryBaseDelay: MUST be > TimeSpan.Zero
- RetryMaxDelay: MUST be ≥ RetryBaseDelay
```

**Example**:
```csharp
var options = new OnePasswordClientOptions
{
    ConnectServer = "https://connect.1password.com",
    Token = "op_1234567890abcdef",
    Timeout = TimeSpan.FromSeconds(60),
    MaxRetries = 5,
    CircuitBreakerFailureThreshold = 3,      // Open after 3 failures
    CircuitBreakerBreakDuration = TimeSpan.FromSeconds(20),
    RetryBaseDelay = TimeSpan.FromMilliseconds(500)
};
```

---

### CircuitBreakerOpenException (New)

**Purpose**: Custom exception thrown when circuit breaker opens during batch operations (FR-016)

**Properties**:

| Property | Type | Description |
|----------|------|-------------|
| `Message` | string | Human-readable error message |
| `PartialResults` | IDictionary<string, string> | Successfully fetched secrets before circuit opened |
| `FailedReferences` | IList<string> | Secret references that failed due to open circuit |
| `InnerException` | Exception? | Original BrokenCircuitException from Polly |

**Relationships**:
- Thrown by `GetSecretsAsync` when circuit opens mid-batch
- Inherits from `OnePasswordException`

**Example**:
```csharp
try
{
    var secrets = await client.GetSecretsAsync(references);
}
catch (CircuitBreakerOpenException ex)
{
    Console.WriteLine($"Partial success: {ex.PartialResults.Count} fetched");
    Console.WriteLine($"Failed due to circuit: {ex.FailedReferences.Count}");

    // Use partial results
    foreach (var (uri, value) in ex.PartialResults)
    {
        Console.WriteLine($"{uri}: {value}");
    }
}
```

---

### ResiliencePolicyConfiguration (Internal)

**Purpose**: Internal model representing the computed policy configuration passed to Polly

**Properties**:

| Property | Type | Description |
|----------|------|-------------|
| `RetryOptions` | HttpRetryStrategyOptions | Polly v8 retry configuration |
| `CircuitBreakerOptions` | HttpCircuitBreakerStrategyOptions | Polly v8 circuit breaker configuration |
| `TimeoutOptions` | HttpTimeoutStrategyOptions | Polly v8 timeout configuration |

**Relationships**:
- Created by `ResiliencePolicyBuilder.Build(OnePasswordClientOptions)`
- Consumed by `PollyHttpClientBuilderExtensions.AddOnePasswordResilience()`

**Lifecycle**: Created once during HttpClient registration, used by Polly pipeline

**Example** (internal usage):
```csharp
var policyConfig = ResiliencePolicyBuilder.Build(clientOptions);

services.AddHttpClient("OnePasswordClient")
    .AddResilienceHandler("Resilience", (builder, context) =>
    {
        builder
            .AddTimeout(policyConfig.TimeoutOptions)
            .AddRetry(policyConfig.RetryOptions)
            .AddCircuitBreaker(policyConfig.CircuitBreakerOptions);
    });
```

---

## Entity Relationships Diagram

```text
OnePasswordClient
    │
    ├── uses ──> OnePasswordClientOptions
    │               │
    │               ├── properties ──> RetryBaseDelay, MaxRetries, etc.
    │               │
    │               └── validated by ──> Validate() method
    │
    ├── creates ──> ResiliencePolicyConfiguration (internal)
    │               │
    │               └── contains ──> HttpRetryStrategyOptions
    │                                HttpCircuitBreakerStrategyOptions
    │                                HttpTimeoutStrategyOptions
    │
    └── may throw ──> CircuitBreakerOpenException
                        │
                        ├── contains ──> PartialResults (IDictionary<string, string>)
                        └── contains ──> FailedReferences (IList<string>)
```

---

## Configuration Defaults Summary

| Setting | Default Value | Rationale |
|---------|---------------|-----------|
| `MaxRetries` | 3 | Balance between reliability and latency (total attempts: 4) |
| `RetryBaseDelay` | 1 second | Standard HTTP retry baseline (1s, 2s, 4s, 8s with backoff) |
| `RetryMaxDelay` | 30 seconds | Prevent excessive wait times in high retry scenarios |
| `EnableJitter` | true | Prevent thundering herd (FR-015) |
| `CircuitBreakerFailureThreshold` | 5 | FR-014 specification default |
| `CircuitBreakerBreakDuration` | 30 seconds | FR-014 specification default |
| `CircuitBreakerSamplingDuration` | 60 seconds | FR-014 specification default |
| `Timeout` | 30 seconds | Existing default, per-request timeout |

---

## Migration Notes

**Backward Compatibility** (FR-006):
- Existing code using `OnePasswordClientOptions` continues to work without changes
- New properties have sensible defaults and are optional
- `MaxRetries` property behavior is preserved - controls retry count as before

**Breaking Changes**: None

**Deprecations**: None

**New Capabilities**:
- Fine-grained control over circuit breaker behavior
- Configurable retry delays and jitter
- Partial results from batch operations when circuit opens
