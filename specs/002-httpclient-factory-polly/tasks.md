# Tasks: HttpClient Refactoring with Factory and Resilience Patterns

**Input**: Design documents from `/specs/002-httpclient-factory-polly/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Tests are REQUIRED per Constitution Principle IV (Test-Driven Development). All tests MUST be written and FAIL before implementation.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Single library project structure:
- **Source**: `src/OnePassword.Sdk/`
- **Tests**: `tests/OnePassword.Sdk.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and dependency updates

- [x] T001 Add Microsoft.Extensions.Http.Resilience package to src/OnePassword.Sdk/OnePassword.Sdk.csproj
- [x] T002 [P] Add Microsoft.Extensions.DependencyInjection.Abstractions package to src/OnePassword.Sdk/OnePassword.Sdk.csproj
- [x] T003 [P] Add WireMock.Net package to tests/OnePassword.Sdk.Tests/OnePassword.Sdk.Tests.csproj for HTTP mocking
- [x] T004 Create src/OnePassword.Sdk/Resilience/ directory for resilience policy classes
- [x] T005 [P] Create src/OnePassword.Sdk/Extensions/ directory for DI extension methods
- [x] T006 [P] Create tests/OnePassword.Sdk.Tests/TestHelpers/ directory for test utilities

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T007 Extend OnePasswordClientOptions class in src/OnePassword.Sdk/Client/OnePasswordClientOptions.cs with circuit breaker properties (FR-014): CircuitBreakerFailureThreshold (default: 5), CircuitBreakerBreakDuration (default: 30s), CircuitBreakerSamplingDuration (default: 60s)
- [ ] T008 [P] Add retry configuration properties to OnePasswordClientOptions in src/OnePassword.Sdk/Client/OnePasswordClientOptions.cs: RetryBaseDelay (default: 1s), RetryMaxDelay (default: 30s), EnableJitter (default: true)
- [ ] T009 Update OnePasswordClientOptions.Validate() method in src/OnePassword.Sdk/Client/OnePasswordClientOptions.cs to validate new resilience properties (FR-018)
- [ ] T010 [P] Create TransientErrorDetector class in src/OnePassword.Sdk/Resilience/TransientErrorDetector.cs implementing FR-005 and FR-010 (distinguish transient vs permanent errors)
- [ ] T011 [P] Create CircuitBreakerOpenException class in src/OnePassword.Sdk/Exceptions/CircuitBreakerOpenException.cs with PartialResults and FailedReferences properties (FR-016)
- [ ] T012 Create SimulatedFailureHttpMessageHandler test helper in tests/OnePassword.Sdk.Tests/TestHelpers/SimulatedFailureHttpMessageHandler.cs for mocking HTTP failures

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Reliable HTTP Communication (Priority: P1) 🎯 MVP

**Goal**: Replace manual retry logic with IHttpClientFactory and Polly-based retry policies, enabling automatic retry with exponential backoff for transient failures

**Independent Test**: Simulate network failures (503, timeout) and verify requests automatically retry with proper backoff. Verify connection reuse through pooling metrics.

### Tests for User Story 1 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T013 [P] [US1] Unit test for TransientErrorDetector in tests/OnePassword.Sdk.Tests/Unit/TransientErrorDetectorTests.cs - verify 408, 429, 500, 502, 503, 504 are transient; 401, 403, 404 are permanent
- [ ] T014 [P] [US1] Unit test for OptionsValidation in tests/OnePassword.Sdk.Tests/Unit/OptionsValidationTests.cs - verify FR-018 (reject negative MaxRetries, zero Timeout, invalid thresholds)
- [ ] T015 [P] [US1] Integration test for RetryBehavior in tests/OnePassword.Sdk.Tests/Integration/RetryBehaviorTests.cs - simulate 503 errors, verify exponential backoff with jitter up to MaxRetries
- [ ] T016 [P] [US1] Integration test for ConnectionPooling in tests/OnePassword.Sdk.Tests/Integration/RetryBehaviorTests.cs - verify same HttpClient instance reused across requests
- [ ] T017 [P] [US1] Integration test for TimeoutHandling in tests/OnePassword.Sdk.Tests/Integration/RetryBehaviorTests.cs - verify FR-017 (skip retries if timeout budget exceeded)

### Implementation for User Story 1

- [ ] T018 [US1] Create ResiliencePolicyBuilder class in src/OnePassword.Sdk/Resilience/ResiliencePolicyBuilder.cs - build HttpRetryStrategyOptions from OnePasswordClientOptions (FR-003, FR-015: exponential backoff with jitter)
- [ ] T019 [US1] Refactor OnePasswordClient.CreateHttpClient() in src/OnePassword.Sdk/Client/OnePasswordClient.cs to use IHttpClientFactory instead of direct instantiation (FR-001)
- [ ] T020 [US1] Refactor OnePasswordClient.SendRequestAsync() in src/OnePassword.Sdk/Client/OnePasswordClient.cs to remove manual retry logic (replace with Polly policies)
- [ ] T021 [US1] Add structured logging to OnePasswordClient.SendRequestAsync() in src/OnePassword.Sdk/Client/OnePasswordClient.cs (FR-009: Warning for retries, Information for success, Debug for policy details)
- [ ] T022 [US1] Update OnePasswordClient.Dispose() in src/OnePassword.Sdk/Client/OnePasswordClient.cs to implement graceful shutdown (FR-011: allow in-flight requests to complete, reject new requests with ObjectDisposedException)

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently - retry with exponential backoff works, connection pooling enabled, backward compatibility maintained

---

## Phase 4: User Story 2 - Circuit Breaker Protection (Priority: P2)

**Goal**: Add circuit breaker pattern to prevent cascading failures when the API is consistently unavailable, enabling fast failure and graceful degradation

**Independent Test**: Simulate sustained API failures and verify circuit opens after threshold is reached. Verify circuit transitions through open → half-open → closed states.

### Tests for User Story 2 ⚠️

- [ ] T023 [P] [US2] Integration test for CircuitBreakerStateTransitions in tests/OnePassword.Sdk.Tests/Integration/CircuitBreakerTests.cs - verify closed → open transition after consecutive failures (FR-014 threshold)
- [ ] T024 [P] [US2] Integration test for CircuitBreakerHalfOpen in tests/OnePassword.Sdk.Tests/Integration/CircuitBreakerTests.cs - verify open → half-open transition after break duration, test request behavior
- [ ] T025 [P] [US2] Integration test for CircuitBreakerRecovery in tests/OnePassword.Sdk.Tests/Integration/CircuitBreakerTests.cs - verify half-open → closed transition after successful test request
- [ ] T026 [P] [US2] Integration test for CircuitBreakerIsolation in tests/OnePassword.Sdk.Tests/Integration/CircuitBreakerTests.cs - verify FR-013 (per-named-client state tracking, not global)
- [ ] T027 [P] [US2] Integration test for BatchOperationCircuitBreaker in tests/OnePassword.Sdk.Tests/Integration/BatchOperationResilienceTests.cs - verify FR-016 (partial results when circuit opens mid-batch)

### Implementation for User Story 2

- [ ] T028 [US2] Add circuit breaker configuration to ResiliencePolicyBuilder in src/OnePassword.Sdk/Resilience/ResiliencePolicyBuilder.cs - build HttpCircuitBreakerStrategyOptions from OnePasswordClientOptions (FR-004, FR-014)
- [ ] T029 [US2] Update OnePasswordClient.GetSecretsAsync() in src/OnePassword.Sdk/Client/OnePasswordClient.cs to handle circuit breaker open during batch operations (FR-016: return partial results, throw CircuitBreakerOpenException with failed references)
- [ ] T030 [US2] Add circuit breaker state change logging in src/OnePassword.Sdk/Resilience/ResiliencePolicyBuilder.cs (FR-009: Warning level for open/close/half-open transitions)
- [ ] T031 [US2] Test circuit breaker integration with retry policy in src/OnePassword.Sdk/Resilience/ResiliencePolicyBuilder.cs - verify strategies execute in correct order (timeout → retry → circuit breaker)

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently - retry with circuit breaker protection, graceful degradation when API is down

---

## Phase 5: User Story 3 - Configurable Resilience Policies (Priority: P3)

**Goal**: Enable developers to configure retry attempts, timeouts, and circuit breaker thresholds for different deployment scenarios (dev vs production)

**Independent Test**: Configure custom policy values and verify they are honored during request execution (custom MaxRetries, custom circuit breaker threshold, custom timeouts)

### Tests for User Story 3 ⚠️

- [ ] T032 [P] [US3] Integration test for CustomRetryConfiguration in tests/OnePassword.Sdk.Tests/Integration/RetryBehaviorTests.cs - verify custom MaxRetries honored
- [ ] T033 [P] [US3] Integration test for CustomCircuitBreakerThreshold in tests/OnePassword.Sdk.Tests/Integration/CircuitBreakerTests.cs - verify custom failure threshold honored
- [ ] T034 [P] [US3] Integration test for CustomTimeoutConfiguration in tests/OnePassword.Sdk.Tests/Integration/RetryBehaviorTests.cs - verify custom timeout duration cancels requests appropriately
- [ ] T035 [P] [US3] Integration test for ServiceCollectionRegistration in tests/OnePassword.Sdk.Tests/Integration/ServiceCollectionTests.cs - verify AddOnePasswordClient() DI registration (FR-012)
- [ ] T036 [P] [US3] Integration test for BackwardCompatibility in tests/OnePassword.Sdk.Tests/Integration/BackwardCompatibilityTests.cs - verify SC-001 (all existing tests pass without modification)

### Implementation for User Story 3

- [ ] T037 [US3] Create ServiceCollectionExtensions class in src/OnePassword.Sdk/Extensions/ServiceCollectionExtensions.cs with AddOnePasswordClient(Action<OnePasswordClientOptions>) method (FR-012, SC-005)
- [ ] T038 [US3] Add AddOnePasswordClient(OnePasswordClientOptions) overload in src/OnePassword.Sdk/Extensions/ServiceCollectionExtensions.cs for pre-configured options
- [ ] T039 [US3] Create PollyHttpClientBuilderExtensions class in src/OnePassword.Sdk/Resilience/PollyHttpClientBuilderExtensions.cs with AddOnePasswordResilience() extension method
- [ ] T040 [US3] Integrate ResiliencePolicyBuilder with IHttpClientFactory in src/OnePassword.Sdk/Extensions/ServiceCollectionExtensions.cs - configure named HttpClient with resilience pipeline (timeout → retry → circuit breaker)
- [ ] T041 [US3] Add telemetry hooks for monitoring integration in src/OnePassword.Sdk/Resilience/ResiliencePolicyBuilder.cs (enable consumers to integrate with their observability systems)

**Checkpoint**: All user stories should now be independently functional - developers can use manual instantiation (backward compatible) or DI registration with custom configuration

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T042 [P] Update quickstart.md validation - verify all examples work with new DI pattern
- [ ] T043 [P] Code cleanup in src/OnePassword.Sdk/Client/OnePasswordClient.cs - remove obsolete manual retry code
- [ ] T044 [P] Performance validation - verify SC-002 (50% connection overhead reduction), SC-004 (circuit breaker <100ms response), SC-006 (no memory leaks), SC-007 (circuit state transition timing)
- [ ] T045 [P] Security review - verify FR-009 log sanitization (no secrets logged at any level)
- [ ] T046 [P] Unit test for ResiliencePolicyBuilder in tests/OnePassword.Sdk.Tests/Unit/ResiliencePolicyBuilderTests.cs - verify policy configuration from options
- [ ] T047 [P] Documentation updates in README.md or API docs - document new DI registration pattern, configuration options
- [ ] T048 Run all existing integration tests to verify SC-001 (backward compatibility - no breaking changes)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion
  - User Story 1 (P1): Can start after Foundational - No dependencies on other stories
  - User Story 2 (P2): Can start after Foundational - **Depends on US1 completion** (needs retry policy integration)
  - User Story 3 (P3): Can start after Foundational - **Depends on US1 and US2 completion** (needs both policies for DI registration)
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after US1 completion - Integrates circuit breaker with existing retry policy
- **User Story 3 (P3)**: Can start after US1 and US2 completion - Exposes full resilience pipeline through DI

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Configuration/models before policy builders
- Policy builders before client refactoring
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel (T002, T003, T005, T006)
- All Foundational tasks marked [P] can run in parallel (T008, T010, T011)
- All tests for a user story marked [P] can run in parallel (after Phase 2 complete)
  - US1 tests: T013-T017 in parallel
  - US2 tests: T023-T027 in parallel
  - US3 tests: T032-T036 in parallel
- Polish tasks marked [P] can run in parallel (T042-T047)
- **NOTE**: User stories CANNOT run in parallel - they have sequential dependencies (US1 → US2 → US3)

---

## Parallel Example: User Story 1

```bash
# After Phase 2 completes, launch all tests for User Story 1 together:
Task: "Unit test for TransientErrorDetector in tests/OnePassword.Sdk.Tests/Unit/TransientErrorDetectorTests.cs"
Task: "Unit test for OptionsValidation in tests/OnePassword.Sdk.Tests/Unit/OptionsValidationTests.cs"
Task: "Integration test for RetryBehavior in tests/OnePassword.Sdk.Tests/Integration/RetryBehaviorTests.cs"
Task: "Integration test for ConnectionPooling in tests/OnePassword.Sdk.Tests/Integration/RetryBehaviorTests.cs"
Task: "Integration test for TimeoutHandling in tests/OnePassword.Sdk.Tests/Integration/RetryBehaviorTests.cs"

# After tests pass, implement sequentially (tasks have dependencies):
Task: "Create ResiliencePolicyBuilder" (T018)
Task: "Refactor CreateHttpClient" (T019)
Task: "Refactor SendRequestAsync" (T020)
Task: "Add structured logging" (T021)
Task: "Update Dispose" (T022)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T006)
2. Complete Phase 2: Foundational (T007-T012) - **CRITICAL GATE**
3. Complete Phase 3: User Story 1 (T013-T022)
4. **STOP and VALIDATE**: Test User Story 1 independently - retry works, connection pooling enabled, backward compatible
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP - retry with exponential backoff!)
3. Add User Story 2 → Test independently → Deploy/Demo (Circuit breaker protection added)
4. Add User Story 3 → Test independently → Deploy/Demo (Full DI support with configuration)
5. Each story adds value without breaking previous stories

### Sequential Execution Strategy

Due to user story dependencies (US1 → US2 → US3), execute in priority order:

1. Team completes Setup + Foundational together
2. Complete User Story 1 (P1) - foundation for all resilience
3. Complete User Story 2 (P2) - adds circuit breaker to retry policy
4. Complete User Story 3 (P3) - exposes full resilience stack via DI
5. Complete Polish phase

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Tests MUST be written first and FAIL before implementation (TDD requirement from Constitution)
- Each user story builds on the previous one (sequential dependencies)
- Verify tests fail before implementing
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, implementing before tests pass

---

## Task Count Summary

- **Phase 1 (Setup)**: 6 tasks
- **Phase 2 (Foundational)**: 6 tasks
- **Phase 3 (User Story 1)**: 10 tasks (5 tests + 5 implementation)
- **Phase 4 (User Story 2)**: 9 tasks (5 tests + 4 implementation)
- **Phase 5 (User Story 3)**: 10 tasks (5 tests + 5 implementation)
- **Phase 6 (Polish)**: 7 tasks
- **Total**: 48 tasks

**Parallel Opportunities Identified**: 18 tasks marked [P] can run in parallel within their phases

**Independent Test Criteria**:
- User Story 1: Simulate 503 errors, verify retry with backoff, verify connection reuse
- User Story 2: Simulate sustained failures, verify circuit state transitions (closed/open/half-open/closed)
- User Story 3: Configure custom values, verify honored in execution; test DI registration

**Suggested MVP Scope**: User Story 1 only (10 tasks after foundation) - delivers automatic retry with exponential backoff, the core value proposition
