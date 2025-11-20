# Implementation Plan: HttpClient Refactoring with Factory and Resilience Patterns

**Branch**: `002-httpclient-factory-polly` | **Date**: 2025-11-19 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-httpclient-factory-polly/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Refactor the OnePassword SDK's HttpClient instantiation to use IHttpClientFactory with Polly-based resilience policies (retry with exponential backoff, circuit breaker). This eliminates manual retry logic in SendRequestAsync, improves connection pooling, and provides production-grade fault tolerance while maintaining backward compatibility with existing public APIs.

## Technical Context

**Language/Version**: C# / .NET 8.0 (multi-target: net8.0)
**Primary Dependencies**:
- Microsoft.Extensions.Http (IHttpClientFactory)
- Polly 8.4.2 (already in csproj)
- Polly.Extensions.Http 3.0.0 (already in csproj)
- Microsoft.Extensions.Logging.Abstractions 8.0.0 (already in csproj)
- Microsoft.Extensions.DependencyInjection.Abstractions (for IServiceCollection extensions)

**Storage**: N/A (library does not persist data)
**Testing**: xUnit with Moq for mocking, integration tests with WireMock for HTTP simulation
**Target Platform**: Cross-platform .NET library (Windows, Linux, macOS)
**Project Type**: Single library project (src/OnePassword.Sdk)
**Performance Goals**:
- Reduce connection establishment overhead by 50% through pooling (SC-002)
- Circuit breaker response within 100ms of threshold (SC-004)
- No memory leaks under sustained load (SC-006)

**Constraints**:
- MUST maintain backward compatibility - no breaking changes to public API (FR-006, SC-001)
- MUST not log secrets at any log level (Constitution: Security-First)
- MUST complete graceful shutdown within reasonable grace period (FR-011)

**Scale/Scope**: Library serving multiple consumers; refactoring affects all HTTP operations (vaults, items, secrets, batch operations)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Security-First Development (Principle I)

✅ **PASS** - This refactoring does not change secret handling logic. Polly policies operate on HTTP requests/responses without inspecting or logging sensitive payloads. Existing secure communication (HTTPS, token handling) remains unchanged.

**Verification**:
- FR-009 explicitly requires log sanitization (Warning/Info/Debug levels, no secrets)
- Circuit breaker and retry policies do not access request/response bodies
- Authentication token remains in HttpClient default headers (existing secure pattern)

### Library-First Architecture (Principle II)

✅ **PASS** - IHttpClientFactory is a .NET framework-agnostic abstraction. Consumers can either:
1. Use direct instantiation (existing pattern) - no breaking changes
2. Use DI registration via new AddOnePasswordClient() extension (FR-012, SC-005)

**Verification**:
- FR-012 requires IServiceCollection support for DI consumers
- FR-006 preserves existing constructor-based instantiation
- Polly and Microsoft.Extensions.Http are industry-standard, well-maintained dependencies

### API Simplicity & Developer Experience (Principle III)

✅ **PASS** - Public API remains unchanged (FR-006). Internal refactoring improves reliability transparently. New DI extension method follows .NET conventions (AddOnePasswordClient).

**Verification**:
- FR-006 mandates backward compatibility
- FR-009 requires structured logging with appropriate levels for troubleshooting
- SC-005 requires single-method DI registration

### Test-Driven Development (Principle IV)

✅ **PASS** - Feature spec defines comprehensive acceptance scenarios for all three user stories. Tests MUST cover:
- Retry behavior under transient failures (503, timeout, network errors)
- Circuit breaker state transitions (closed → open → half-open → closed)
- Configuration validation (FR-018)
- Batch operation partial results (FR-016)
- Graceful shutdown (FR-011)

**Verification**:
- SC-001 requires all existing integration tests to pass
- Each functional requirement has corresponding testable acceptance scenario
- Edge cases explicitly defined for test coverage

### Observability & Diagnostics (Principle V)

✅ **PASS** - FR-009 mandates structured logging at appropriate levels:
- Warning: Retry attempts, circuit breaker state changes
- Information: Successful operations
- Debug: Policy execution details

**Verification**:
- Existing CorrelationContext.GetCorrelationId() pattern continues
- Polly policies support telemetry hooks for monitoring integration
- Log output must be sanitized (no secrets)

### Compliance Summary

**All gates PASSED** - No constitution violations. Feature aligns with all five core principles.

## Project Structure

### Documentation (this feature)

```text
specs/002-httpclient-factory-polly/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (Polly v8 patterns, HttpClientFactory best practices)
├── data-model.md        # Phase 1 output (resilience policy configuration model)
├── quickstart.md        # Phase 1 output (developer migration guide)
├── contracts/           # Phase 1 output (resilience policy interfaces, configuration contracts)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── OnePassword.Sdk/
│   ├── Client/
│   │   ├── OnePasswordClient.cs          # MODIFIED: Replace CreateHttpClient(), refactor SendRequestAsync
│   │   ├── IOnePasswordClient.cs         # No changes (public interface preserved)
│   │   └── OnePasswordClientOptions.cs   # MODIFIED: Add circuit breaker config properties (FR-014)
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs # NEW: AddOnePasswordClient() DI registration (FR-012)
│   ├── Resilience/                        # NEW: Polly policy configuration
│   │   ├── ResiliencePolicyBuilder.cs     # NEW: Builds retry + circuit breaker policies
│   │   ├── PollyHttpClientBuilderExtensions.cs # NEW: Configure named HttpClient with policies
│   │   └── TransientErrorDetector.cs      # NEW: Implements FR-005 (transient vs permanent errors)
│   ├── Exceptions/
│   │   └── CircuitBreakerOpenException.cs # NEW: For FR-016 (batch operation partial results)
│   └── OnePassword.Sdk.csproj            # MODIFIED: Add Microsoft.Extensions.DependencyInjection.Abstractions

tests/
├── OnePassword.Sdk.Tests/
│   ├── Unit/
│   │   ├── ResiliencePolicyBuilderTests.cs # NEW: Test policy configuration
│   │   ├── TransientErrorDetectorTests.cs  # NEW: Test FR-005 error classification
│   │   └── OptionsValidationTests.cs       # NEW: Test FR-018 configuration validation
│   ├── Integration/
│   │   ├── RetryBehaviorTests.cs           # NEW: Test retry with exponential backoff
│   │   ├── CircuitBreakerTests.cs          # NEW: Test circuit breaker state transitions
│   │   ├── BatchOperationResilienceTests.cs # NEW: Test FR-016 partial results
│   │   ├── ServiceCollectionTests.cs       # NEW: Test DI registration (FR-012)
│   │   └── BackwardCompatibilityTests.cs   # NEW: Verify SC-001 (existing tests pass)
│   └── TestHelpers/
│       └── SimulatedFailureHttpMessageHandler.cs # NEW: Mock HTTP failures for testing
```

**Structure Decision**: Single library project (existing structure). New `Resilience/` namespace isolates policy configuration logic. DI extensions follow .NET convention (Extensions/ namespace). Tests organized by unit (policy logic) vs integration (end-to-end behavior).

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No violations - this section intentionally empty.
