# Feature Specification: HttpClient Refactoring with Factory and Resilience Patterns

**Feature Branch**: `002-httpclient-factory-polly`
**Created**: 2025-11-19
**Status**: Draft
**Input**: User description: "Refactor HttpClient to use HttpClientFactory with Polly for retry and circuit breaker"

## Clarifications

### Session 2025-11-19

- Q: What happens when the circuit breaker opens during a batch operation (GetSecretsAsync with multiple items)? → A: Return partial results for items fetched before circuit opened, throw exception indicating circuit open for remaining items
- Q: What log levels should be used for retry attempts, circuit breaker state changes, and policy executions? → A: Retry attempts at Warning, circuit state changes at Warning, successful operations at Information, policy execution details at Debug
- Q: How does the system handle requests when HttpClientFactory is disposed or the application is shutting down? → A: Allow in-flight requests to complete (up to a grace period), reject new requests with ObjectDisposedException
- Q: What occurs when retry delays exceed the overall request timeout? → A: Respect overall timeout - complete current attempt but skip remaining retries if timeout would be exceeded, throw TimeoutException
- Q: What happens when configuration values are invalid (negative retry counts, zero timeouts)? → A: Validate configuration at client initialization, throw ArgumentException with details of invalid values

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Reliable HTTP Communication (Priority: P1)

As a developer using the OnePassword SDK, I need the HTTP client to automatically handle transient failures so that my application remains resilient without manual retry logic.

**Why this priority**: This is the foundation of the refactoring - replacing manual retry logic with a standardized, tested resilience framework ensures all SDK users benefit from production-grade error handling.

**Independent Test**: Can be fully tested by simulating network failures and verifying that requests automatically retry with proper backoff, and delivers immediate value by improving reliability of all API calls.

**Acceptance Scenarios**:

1. **Given** the 1Password API returns a 503 Service Unavailable error, **When** the SDK makes a request, **Then** the request automatically retries with exponential backoff up to the configured maximum attempts
2. **Given** a network timeout occurs, **When** the SDK makes a request, **Then** the request is retried according to the retry policy without application intervention
3. **Given** multiple consecutive requests to the same endpoint, **When** all requests complete successfully, **Then** the same HttpClient instance is reused (verified through connection pooling metrics)

---

### User Story 2 - Circuit Breaker Protection (Priority: P2)

As a developer using the OnePassword SDK, I need the client to stop making requests when the service is consistently failing so that my application doesn't waste resources and can fail fast.

**Why this priority**: Prevents cascading failures and resource exhaustion when the API is down, enabling applications to degrade gracefully.

**Independent Test**: Can be tested by simulating sustained API failures and verifying the circuit opens after threshold is reached, preventing further requests until the circuit resets.

**Acceptance Scenarios**:

1. **Given** the 1Password API fails consecutively for the configured threshold, **When** subsequent requests are attempted, **Then** the circuit breaker opens and requests fail immediately without hitting the API
2. **Given** the circuit breaker is in open state, **When** the configured duration elapses, **Then** the circuit transitions to half-open and allows a test request through
3. **Given** the circuit breaker is in half-open state, **When** a test request succeeds, **Then** the circuit closes and normal operation resumes

---

### User Story 3 - Configurable Resilience Policies (Priority: P3)

As a developer using the OnePassword SDK, I need to configure retry attempts, timeouts, and circuit breaker thresholds so that I can tune the resilience behavior for my specific environment and requirements.

**Why this priority**: Different deployment scenarios have different needs - development environments may want fast failures while production needs more retries.

**Independent Test**: Can be tested by configuring different policy values and verifying they are honored during request execution.

**Acceptance Scenarios**:

1. **Given** custom retry count is configured in OnePasswordClientOptions, **When** a transient failure occurs, **Then** the system retries up to the specified count
2. **Given** custom timeout duration is configured, **When** a request takes longer than the timeout, **Then** the request is cancelled and treated as a transient failure
3. **Given** custom circuit breaker threshold is configured, **When** failures occur, **Then** the circuit opens after the specified number of consecutive failures

---

### Edge Cases

- When the circuit breaker opens during a batch operation (GetSecretsAsync with multiple items), the system returns partial results for items fetched before the circuit opened and throws an exception indicating the circuit is open for remaining items
- When HttpClientFactory is disposed or the application is shutting down, in-flight requests are allowed to complete up to a grace period, while new requests are rejected with ObjectDisposedException
- When retry delays would exceed the overall request timeout, the system completes the current attempt but skips remaining retries and throws TimeoutException
- Non-transient errors (401, 403, 404) bypass retry logic entirely as specified in FR-010
- Invalid configuration values (negative retry counts, zero timeouts) are validated at client initialization and throw ArgumentException with details of invalid values

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST create HttpClient instances using IHttpClientFactory instead of direct instantiation
- **FR-002**: System MUST configure resilience policies using Polly for transient error handling
- **FR-003**: System MUST implement exponential backoff retry policy for transient HTTP failures
- **FR-004**: System MUST implement circuit breaker pattern to prevent request cascades during sustained failures
- **FR-005**: System MUST distinguish between transient errors (retryable) and permanent errors (non-retryable)
- **FR-006**: System MUST preserve existing public API signatures and behavior for backward compatibility
- **FR-007**: System MUST allow configuration of retry count, timeout duration, and circuit breaker thresholds
- **FR-008**: System MUST maintain the existing MaxRetries setting in OnePasswordClientOptions for backward compatibility
- **FR-009**: System MUST log retry attempts and circuit breaker state changes at Warning level, successful operations at Information level, and policy execution details at Debug level
- **FR-010**: System MUST not retry on authentication failures (401), authorization failures (403), or not found errors (404)
- **FR-011**: System MUST dispose of HttpClient instances properly through the factory lifecycle, allowing in-flight requests to complete (with grace period) while rejecting new requests with ObjectDisposedException
- **FR-012**: System MUST support dependency injection patterns for consumers using IServiceCollection
- **FR-013**: Circuit breaker MUST track failures per named HttpClient policy, not globally across all clients
- **FR-014**: System MUST expose circuit breaker thresholds as configurable properties in OnePasswordClientOptions: consecutive failure threshold (default: 5), break duration (default: 30 seconds), and sampling duration for tracking failures (default: 60 seconds)
- **FR-015**: Retry policy MUST use jittered exponential backoff to prevent thundering herd scenarios
- **FR-016**: During batch operations (GetSecretsAsync), if the circuit breaker opens mid-execution, the system MUST return partial results for successfully fetched items and include a circuit breaker exception with details of unfetched items
- **FR-017**: When cumulative retry delays would exceed the overall request timeout, the system MUST complete the current attempt, skip remaining retries, and throw TimeoutException
- **FR-018**: System MUST validate all configuration values at client initialization and throw ArgumentException with descriptive error messages for invalid values (negative retry counts, zero/negative timeouts, invalid thresholds)

### Key Entities

- **HttpClient**: Managed by HttpClientFactory for proper lifecycle and connection pooling
- **Resilience Policy**: Polly-based policies defining retry and circuit breaker behavior
- **OnePasswordClientOptions**: Configuration object containing resilience policy settings
- **Named HttpClient**: Registered with specific resilience policies for 1Password API calls

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All existing integration tests pass without modification, demonstrating backward compatibility
- **SC-002**: HTTP connections are reused across requests, reducing connection establishment overhead by at least 50% compared to manual HttpClient creation
- **SC-003**: Transient failures are automatically retried without application code changes, with retry attempts visible in logs
- **SC-004**: When the API experiences sustained failures, the circuit breaker prevents request attempts within 100ms of the threshold being reached
- **SC-005**: Applications using dependency injection can register the SDK client with a single AddOnePasswordClient() extension method
- **SC-006**: Memory usage remains stable under sustained load with no HttpClient resource leaks (verified through memory profiling)
- **SC-007**: Circuit breaker state transitions (closed → open → half-open → closed) complete within expected timeframes under test conditions

## Assumptions *(mandatory)*

- The existing OnePasswordClientOptions class can be extended with new properties without breaking changes
- Applications using the SDK have access to Microsoft.Extensions.Http package (standard in .NET 8.0 applications)
- Polly v8 or later is acceptable as a dependency
- Current manual retry logic in SendRequestAsync method will be completely replaced by Polly policies
- The IDisposable implementation on OnePasswordClient will no longer need to dispose HttpClient directly
- Existing tests assume manual retry behavior and may need updates to work with policy-based retries
- Circuit breaker state should be scoped to the named HttpClient, not shared across multiple client instances
- Default configuration values will provide sensible production-ready behavior
