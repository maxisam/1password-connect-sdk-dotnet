# Implementation Tasks: 1Password .NET SDK

**Feature**: 001-onepassword-sdk
**Branch**: `001-onepassword-sdk`
**Generated**: 2025-11-18
**Source Documents**: spec.md, plan.md, data-model.md, contracts/, research.md

## Overview

This task breakdown enables incremental delivery of the 1Password .NET SDK. Each user story phase represents an independently testable increment that delivers complete user value.

**User Stories** (from spec.md):
- **User Story 1 (P1)**: Programmatic Vault Access - Core SDK functionality
- **User Story 2 (P2)**: Configuration Builder Integration - op:// URI resolution
- **User Story 3 (P3)**: Environment Variable Override - Configuration precedence

**MVP Scope**: User Story 1 only (complete, standalone SDK library)

**Test Strategy**: Tests follow from spec requirements, not TDD. Tests are integrated throughout based on acceptance scenarios.

---

## Implementation Strategy

### Incremental Delivery

1. **MVP (User Story 1)**: Deliver core SDK (`OnePassword.Sdk` package)
   - Independently usable for programmatic secret retrieval
   - Complete exception hierarchy, logging, retry logic
   - Fully tested against acceptance scenarios

2. **Enhancement (User Story 2)**: Add configuration provider (`OnePassword.Configuration` package)
   - Builds on core SDK (dependency)
   - Adds op:// URI parsing and batch resolution
   - Configuration integration use cases

3. **Polish (User Story 3)**: Environment variable precedence validation
   - Leverages existing .NET configuration precedence
   - Validation and testing of override behavior

### Parallel Execution Opportunities

Tasks marked with `[P]` can be executed in parallel when their prerequisites are met. See "Parallel Execution Examples" section for specific parallel task groups per phase.

---

## Phase 1: Project Setup

**Goal**: Initialize .NET solution structure with packages and foundational dependencies

**Prerequisites**: None

### Tasks

- [ ] T001 Create solution file and .gitignore at repository root
  - Run: `dotnet new sln -n OnePassword`
  - Create .gitignore for .NET (bin/, obj/, *.user, etc.)
  - **Files**: `OnePassword.sln`, `.gitignore`

- [ ] T002 [P] Create OnePassword.Sdk class library project
  - Run: `dotnet new classlib -n OnePassword.Sdk -f net8.0 -o src/OnePassword.Sdk`
  - Target frameworks: net8.0;net6.0 (multi-targeting)
  - Add to solution: `dotnet sln add src/OnePassword.Sdk`
  - **Files**: `src/OnePassword.Sdk/OnePassword.Sdk.csproj`

- [ ] T003 [P] Create OnePassword.Configuration class library project
  - Run: `dotnet new classlib -n OnePassword.Configuration -f net8.0 -o src/OnePassword.Configuration`
  - Target frameworks: net8.0;net6.0
  - Add to solution: `dotnet sln add src/OnePassword.Configuration`
  - **Files**: `src/OnePassword.Configuration/OnePassword.Configuration.csproj`

- [ ] T004 [P] Create OnePassword.Sdk.Tests test project
  - Run: `dotnet new xunit -n OnePassword.Sdk.Tests -o tests/OnePassword.Sdk.Tests`
  - Add to solution: `dotnet sln add tests/OnePassword.Sdk.Tests`
  - **Files**: `tests/OnePassword.Sdk.Tests/OnePassword.Sdk.Tests.csproj`

- [ ] T005 [P] Create OnePassword.Configuration.Tests test project
  - Run: `dotnet new xunit -n OnePassword.Configuration.Tests -o tests/OnePassword.Configuration.Tests`
  - Add to solution: `dotnet sln add tests/OnePassword.Configuration.Tests`
  - **Files**: `tests/OnePassword.Configuration.Tests/OnePassword.Configuration.Tests.csproj`

- [ ] T006 [P] Create OnePassword.Integration.Tests test project
  - Run: `dotnet new xunit -n OnePassword.Integration.Tests -o tests/OnePassword.Integration.Tests`
  - Add to solution: `dotnet sln add tests/OnePassword.Integration.Tests`
  - **Files**: `tests/OnePassword.Integration.Tests/OnePassword.Integration.Tests.csproj`

- [ ] T007 Add NuGet dependencies to OnePassword.Sdk project
  - Add: Microsoft.Extensions.Logging.Abstractions (latest stable)
  - Add: System.Text.Json (if not included in .NET 8/6)
  - Add: Polly (latest v8 for resilience)
  - Add: Polly.Extensions.Http (for HttpClient integration)
  - **Files**: `src/OnePassword.Sdk/OnePassword.Sdk.csproj`

- [ ] T008 Add NuGet dependencies to OnePassword.Configuration project
  - Add project reference: OnePassword.Sdk
  - Add: Microsoft.Extensions.Configuration.Abstractions (latest stable)
  - **Files**: `src/OnePassword.Configuration/OnePassword.Configuration.csproj`

- [ ] T009 [P] Add test dependencies to all test projects
  - Add to each test project:
    - xUnit (should be present from template)
    - FluentAssertions (latest)
    - Moq (latest)
    - AutoFixture.Xunit2 (for test data generation per plan.md)
    - Microsoft.NET.Test.Sdk (for test runner)
  - **Files**: All test project .csproj files

- [ ] T010 Create directory structure per plan.md
  - Create: src/OnePassword.Sdk/{Client,Models,Exceptions,Internal}
  - Create: src/OnePassword.Configuration/Internal
  - Create: tests/OnePassword.Sdk.Tests/{Client,Models,Internal}
  - Create: tests/OnePassword.Configuration.Tests/
  - **Files**: Directory structure (no code yet)

- [ ] T011 Configure solution-level settings
  - Create Directory.Build.props for common settings (Nullable enable, TreatWarningsAsErrors)
  - Add .editorconfig for C# code style
  - **Files**: `Directory.Build.props`, `.editorconfig`

**Parallel Execution (Phase 1)**:
- Group A (after T001): T002, T003, T004, T005, T006 (all project creation)
- Group B (after T002-T006): T007, T008, T009 (NuGet dependencies)

---

## Phase 2: Foundational Components

**Goal**: Implement shared infrastructure needed by all user stories

**Prerequisites**: Phase 1 complete

### Tasks

- [ ] T012 [P] Implement base OnePasswordException class
  - Create: src/OnePassword.Sdk/Exceptions/OnePasswordException.cs
  - Base class with message and inner exception constructors
  - Per contracts/Exceptions.cs specification
  - **Files**: `src/OnePassword.Sdk/Exceptions/OnePasswordException.cs`

- [ ] T013 [P] Implement authentication exceptions
  - Create: AuthenticationException, AccessDeniedException classes
  - Include VaultId, ItemId properties in AccessDeniedException
  - Per contracts/Exceptions.cs
  - **Files**: `src/OnePassword.Sdk/Exceptions/AuthenticationException.cs`, `src/OnePassword.Sdk/Exceptions/AccessDeniedException.cs`

- [ ] T014 [P] Implement resource not found exceptions
  - Create: VaultNotFoundException, ItemNotFoundException, FieldNotFoundException
  - Include context properties (VaultId, ItemId, FieldLabel)
  - Formatted error messages per FR-026
  - **Files**: `src/OnePassword.Sdk/Exceptions/{VaultNotFoundException,ItemNotFoundException,FieldNotFoundException}.cs`

- [ ] T015 [P] Implement network and communication exceptions
  - Create: NetworkException with RetryAttempts property
  - Error message includes retry count
  - **Files**: `src/OnePassword.Sdk/Exceptions/NetworkException.cs`

- [ ] T016 [P] Implement validation and limit exceptions
  - Create: MalformedUriException (ConfigurationKey, MalformedUri, reason)
  - Create: BatchSizeExceededException (RequestedCount, MaximumAllowed)
  - Create: SecretSizeExceededException (VaultId, ItemId, FieldLabel, sizes)
  - Create: BatchTimeoutException (Timeout)
  - Per contracts/Exceptions.cs
  - **Files**: `src/OnePassword.Sdk/Exceptions/{MalformedUriException,BatchSizeExceededException,SecretSizeExceededException,BatchTimeoutException}.cs`

- [ ] T017 [P] Create domain model: Vault
  - Create: src/OnePassword.Sdk/Models/Vault.cs
  - Properties: Id, Name, Description, CreatedAt, UpdatedAt (init-only)
  - JSON serialization attributes (System.Text.Json)
  - Per data-model.md specification
  - **Files**: `src/OnePassword.Sdk/Models/Vault.cs`

- [ ] T018 [P] Create domain model: Item
  - Create: src/OnePassword.Sdk/Models/Item.cs
  - Properties: Id, VaultId, Title, Category, Fields, Sections, CreatedAt, UpdatedAt
  - Immutable with init accessors
  - **Files**: `src/OnePassword.Sdk/Models/Item.cs`

- [ ] T019 [P] Create domain model: Field and FieldType/FieldPurpose enums
  - Create: src/OnePassword.Sdk/Models/Field.cs
  - Properties: Id, Label, Value, Type, Purpose, SectionId
  - Override ToString() to exclude Value (security - no secret logging)
  - Enums: FieldType, FieldPurpose per data-model.md
  - **Files**: `src/OnePassword.Sdk/Models/Field.cs`

- [ ] T020 [P] Create domain model: Section
  - Create: src/OnePassword.Sdk/Models/Section.cs
  - Properties: Id, Label (immutable)
  - **Files**: `src/OnePassword.Sdk/Models/Section.cs`

- [ ] T021 Create OnePasswordClientOptions configuration class
  - Create: src/OnePassword.Sdk/Client/OnePasswordClientOptions.cs
  - Properties: ConnectServer, Token, Timeout (default 10s), MaxRetries (default 3)
  - Validation: ConnectServer must be HTTPS, Token non-empty
  - **Files**: `src/OnePassword.Sdk/Client/OnePasswordClientOptions.cs`

- [ ] T022 Unit test: Exception hierarchy
  - Create: tests/OnePassword.Sdk.Tests/Exceptions/ExceptionTests.cs
  - Test: All exceptions construct with message and context
  - Test: Error messages include expected context (vault/item/field names)
  - Test: Exceptions are serializable (if needed for remoting)
  - **Files**: `tests/OnePassword.Sdk.Tests/Exceptions/ExceptionTests.cs`

- [ ] T023 [P] Unit test: Domain models
  - Create: tests/OnePassword.Sdk.Tests/Models/ModelTests.cs
  - Test: Models are immutable (init-only properties)
  - Test: Field.ToString() does NOT include Value (security)
  - Test: JSON serialization/deserialization round-trips correctly
  - **Files**: `tests/OnePassword.Sdk.Tests/Models/ModelTests.cs`

**Parallel Execution (Phase 2)**:
- Group A (after T011): T012, T013, T014, T015, T016 (all exceptions in parallel)
- Group B (after T011): T017, T018, T019, T020, T021 (all models in parallel)
- Group C (after Group A, B): T022, T023 (tests in parallel)

---

## Phase 3: User Story 1 - Programmatic Vault Access (P1)

**Story Goal**: Deliver core SDK for programmatic 1Password vault access

**Why this priority**: Foundation for all other functionality. Independently usable as standalone library.

**Independent Test Criteria** (from spec.md):
- ✅ Initialize SDK with authentication credentials
- ✅ Connect to 1Password vault
- ✅ Retrieve specific item by name
- ✅ Extract secret field value
- ✅ Secret matches expected value in 1Password

**Acceptance Scenarios** (from spec.md User Story 1):
1. Valid credentials → retrieve correct secret value
2. Non-existent vault/item → clear error indicating what not found
3. Invalid credentials → authentication error with clear message
4. Multiple secrets in item → specific field extracted

### Tasks

- [ ] T024 [US1] Implement IOnePasswordClient interface
  - Create: src/OnePassword.Sdk/Client/IOnePasswordClient.cs
  - Define all methods per contracts/IOnePasswordClient.cs
  - XML documentation comments for all methods
  - **Files**: `src/OnePassword.Sdk/Client/IOnePasswordClient.cs`

- [ ] T025 [US1] Implement HTTP client factory and Polly retry policies
  - Create: src/OnePassword.Sdk/Internal/HttpClientFactory.cs (or use IHttpClientFactory)
  - Configure Polly retry policy: 3 attempts, exponential backoff (1s, 2s, 4s)
  - Configure timeout policy: 10 seconds per request
  - Per research.md Polly decision
  - **Files**: `src/OnePassword.Sdk/Internal/HttpClientFactory.cs` or `src/OnePassword.Sdk/Client/OnePasswordClient.cs` (inline config)

- [ ] T026 [US1] Implement OnePasswordClient constructor and authentication
  - Create: src/OnePassword.Sdk/Client/OnePasswordClient.cs
  - Constructor: Accept OnePasswordClientOptions, ILogger<OnePasswordClient>
  - Validate options (HTTPS URL, non-empty token)
  - Initialize HttpClient with Polly policies
  - Log at INFO: "OnePasswordClient initialized with server {url}"
  - **Files**: `src/OnePassword.Sdk/Client/OnePasswordClient.cs`

- [ ] T027 [US1] Implement vault operations: ListVaultsAsync
  - In OnePasswordClient: Implement ListVaultsAsync method
  - HTTP GET /v1/vaults
  - Deserialize to IEnumerable<Vault>
  - Handle errors: throw AuthenticationException on 401, NetworkException on failures
  - Log at INFO: "Listed {count} vaults successfully"
  - **Files**: `src/OnePassword.Sdk/Client/OnePasswordClient.cs`

- [ ] T028 [US1] Implement vault operations: GetVaultAsync, GetVaultByTitleAsync
  - Implement GetVaultAsync: GET /v1/vaults/{vaultId}
  - Implement GetVaultByTitleAsync: List vaults, filter by title, throw VaultNotFoundException if not found
  - Handle 404 → VaultNotFoundException with context
  - Handle 403 → AccessDeniedException with vault ID
  - **Files**: `src/OnePassword.Sdk/Client/OnePasswordClient.cs`

- [ ] T029 [US1] Implement item operations: ListItemsAsync
  - Implement ListItemsAsync: GET /v1/vaults/{vaultId}/items
  - Deserialize to IEnumerable<Item>
  - Handle errors: VaultNotFoundException, AccessDeniedException
  - Log at INFO: "Listed {count} items from vault {vaultId}"
  - **Files**: `src/OnePassword.Sdk/Client/OnePasswordClient.cs`

- [ ] T030 [US1] Implement item operations: GetItemAsync, GetItemByTitleAsync
  - Implement GetItemAsync: GET /v1/vaults/{vaultId}/items/{itemId}
  - Implement GetItemByTitleAsync: List items, filter by title
  - Validate secret size: throw SecretSizeExceededException if any field >1MB
  - Handle 404 → ItemNotFoundException with context (vault + item)
  - **Files**: `src/OnePassword.Sdk/Client/OnePasswordClient.cs`

- [ ] T031 [US1] Implement GetSecretAsync convenience method
  - Implement GetSecretAsync(vaultId, itemId, fieldLabel)
  - Call GetItemAsync, extract field by label
  - Throw FieldNotFoundException if field not found (include context: vault/item/field)
  - Log at INFO: "Retrieved secret for field {field} from item {item}"
  - Security: Do NOT log the secret value itself
  - **Files**: `src/OnePassword.Sdk/Client/OnePasswordClient.cs`

- [ ] T032 [US1] Implement IDisposable for OnePasswordClient
  - Dispose HttpClient resources
  - Log at INFO: "OnePasswordClient disposed"
  - **Files**: `src/OnePassword.Sdk/Client/OnePasswordClient.cs`

- [ ] T033 [US1] Unit test: OnePasswordClient initialization and validation
  - Create: tests/OnePassword.Sdk.Tests/Client/OnePasswordClientTests.cs
  - Test: Constructor validates HTTPS URL (throw ArgumentException if HTTP)
  - Test: Constructor validates non-empty token
  - Test: Client initializes successfully with valid options
  - Test: Verify INFO-level log emitted: "OnePasswordClient initialized with server {url}" (FR-040)
  - **Files**: `tests/OnePassword.Sdk.Tests/Client/OnePasswordClientTests.cs`

- [ ] T034 [P] [US1] Unit test: Vault operations with mocked HTTP
  - Mock HTTP responses using Moq or custom HttpMessageHandler
  - Test: ListVaultsAsync deserializes response correctly
  - Test: GetVaultAsync handles 404 → VaultNotFoundException
  - Test: GetVaultAsync handles 403 → AccessDeniedException
  - Test: GetVaultByTitleAsync filters correctly, throws if not found
  - **Files**: `tests/OnePassword.Sdk.Tests/Client/VaultOperationsTests.cs`

- [ ] T035 [P] [US1] Unit test: Item operations with mocked HTTP
  - Test: GetItemAsync deserializes Item with Fields correctly
  - Test: GetItemAsync throws ItemNotFoundException on 404
  - Test: GetItemAsync validates field sizes, throws SecretSizeExceededException if >1MB
  - Test: GetItemByTitleAsync filters and throws if not found
  - **Files**: `tests/OnePassword.Sdk.Tests/Client/ItemOperationsTests.cs`

- [ ] T036 [P] [US1] Unit test: GetSecretAsync field extraction
  - Test: GetSecretAsync extracts correct field value from item
  - Test: GetSecretAsync throws FieldNotFoundException if field missing
  - Test: Error message includes vault/item/field context
  - **Files**: `tests/OnePassword.Sdk.Tests/Client/SecretRetrievalTests.cs`

- [ ] T037 [P] [US1] Unit test: Retry and timeout policies
  - Create: tests/OnePassword.Sdk.Tests/Internal/RetryPolicyTests.cs
  - Test: Transient failures (503, network timeout) trigger retry (up to 3 times)
  - Test: Exponential backoff delays (1s, 2s, 4s) between retries
  - Test: After 3 retries, throw NetworkException with retry count
  - Test: Request timeout after 10 seconds → TimeoutException
  - Test: Verify WARN-level log on each retry includes attempt number (e.g., "Retry attempt 2 of 3") (FR-041)
  - **Files**: `tests/OnePassword.Sdk.Tests/Internal/RetryPolicyTests.cs`

- [ ] T038 [US1] Integration test: End-to-end secret retrieval (mocked API)
  - Create: tests/OnePassword.Integration.Tests/EndToEndTests.cs
  - Set up mock 1Password Connect API (in-memory or test server)
  - Test: Full flow: Initialize client → Get vault → Get item → Extract field
  - Test: Acceptance scenario 1: Valid credentials → correct secret
  - Test: Acceptance scenario 2: Non-existent vault → VaultNotFoundException
  - Test: Acceptance scenario 3: Invalid token → AuthenticationException
  - Test: Acceptance scenario 4: Multiple fields → extract specific field
  - **Files**: `tests/OnePassword.Integration.Tests/EndToEndTests.cs`

- [ ] T039 [US1] Create NuGet package metadata for OnePassword.Sdk
  - Edit: src/OnePassword.Sdk/OnePassword.Sdk.csproj
  - Add: PackageId, Version (1.0.0-beta1), Authors, Description, PackageTags
  - Add: RepositoryUrl, PackageLicenseExpression (e.g., MIT)
  - Add: GenerateDocumentationFile (true for XML docs)
  - **Files**: `src/OnePassword.Sdk/OnePassword.Sdk.csproj`

**Parallel Execution (Phase 3)**:
- T024 blocks all others (interface definition)
- Group A (after T026): T027, T029 (vault and item list operations)
- Group B (after T028, T030): T031 (GetSecretAsync depends on GetItemAsync)
- Group C (after T032): T033, T034, T035, T036, T037 (all unit tests in parallel)
- T038 runs after all implementation complete

**Independent Test Validation**: T038 validates all acceptance scenarios from spec.md

---

## Phase 4: User Story 2 - Configuration Builder Integration (P2)

**Story Goal**: Enable automatic resolution of op:// URIs in appsettings.json

**Why this priority**: Key differentiator. Enables "pit of success" pattern for secret management.

**Dependencies**: Requires User Story 1 complete (core SDK)

**Independent Test Criteria** (from spec.md):
- ✅ Create appsettings.json with op:// URIs
- ✅ Add 1Password provider to configuration builder
- ✅ Read configuration keys
- ✅ Verify actual secrets returned (not op:// URIs)

**Acceptance Scenarios** (from spec.md User Story 2):
1. op:// URI in config → resolved to actual password
2. Multiple op:// URIs → all resolved in single batch call
3. Non-op:// values → left unchanged
4. Invalid op:// URI → clear error identifying which reference failed
5. Secret resolution happens after other sources loaded, before config sealed

### Tasks

- [ ] T040 [US2] Create SecretReference model with URI parsing
  - Create: src/OnePassword.Sdk/Models/SecretReference.cs
  - Properties: Vault, Item, Section (optional), Field, OriginalUri
  - Static method: TryParse(string uri, out SecretReference, out string errorMessage)
  - Validation: Must start with "op://", validate component count (3 or 4), URL-decode
  - Per data-model.md specification
  - **Files**: `src/OnePassword.Sdk/Models/SecretReference.cs`

- [ ] T041 [P] [US2] Unit test: SecretReference URI parsing
  - Create: tests/OnePassword.Sdk.Tests/Models/SecretReferenceTests.cs
  - Test: Valid URI "op://vault/item/field" → parses correctly
  - Test: Valid URI with section "op://vault/item/section/field" → parses correctly
  - Test: URL-encoded components "op://vault/my%20item/field" → decoded
  - Test: Invalid prefix "http://..." → TryParse returns false with error message
  - Test: Missing components "op://vault/item/" → TryParse returns false
  - Test: Empty components → TryParse returns false
  - **Files**: `tests/OnePassword.Sdk.Tests/Models/SecretReferenceTests.cs`

- [ ] T042 [US2] Implement batch GetSecretsAsync in OnePasswordClient
  - Add to OnePasswordClient: GetSecretsAsync(IEnumerable<string> references)
  - Parse all URIs using SecretReference.TryParse (fail fast if malformed → MalformedUriException)
  - Validate batch size ≤100 (throw BatchSizeExceededException if exceeded)
  - Deduplicate URIs (same URI fetched only once per FR-019)
  - Group by vault+item, fetch each unique item once
  - Extract requested fields from items, build dictionary mapping URI → secret value
  - Timeout: 10 seconds total (throw BatchTimeoutException if exceeded)
  - Log at INFO: "Batch retrieved {count} secrets in {duration}ms"
  - **Files**: `src/OnePassword.Sdk/Client/OnePasswordClient.cs`

- [ ] T043 [P] [US2] Unit test: GetSecretsAsync batch retrieval
  - Create: tests/OnePassword.Sdk.Tests/Client/BatchRetrievalTests.cs
  - Test: Multiple URIs → all resolved correctly
  - Test: Duplicate URIs → fetched only once (verify HTTP call count)
  - Test: Batch size limit: >100 URIs → BatchSizeExceededException
  - Test: Malformed URI in batch → MalformedUriException before API calls
  - Test: Timeout after 10s → BatchTimeoutException
  - Test: Same vault+item, different fields → single HTTP call
  - **Files**: `tests/OnePassword.Sdk.Tests/Client/BatchRetrievalTests.cs`

- [ ] T044 [US2] Implement OnePasswordConfigurationProvider
  - Create: src/OnePassword.Configuration/OnePasswordConfigurationProvider.cs
  - Inherit from ConfigurationProvider (Microsoft.Extensions.Configuration)
  - Constructor: Accept OnePasswordConfigurationSource, ILogger
  - Override Load(): Scan all keys, collect op:// URIs, batch retrieve, cache secrets
  - Cache: ConcurrentDictionary<string, string> for resolved secrets
  - Override TryGet(key): Return cached secret if key was resolved, else base.TryGet
  - Per data-model.md caching strategy
  - **Files**: `src/OnePassword.Configuration/OnePasswordConfigurationProvider.cs`

- [ ] T045 [US2] Implement Load() method: Scan and collect op:// URIs
  - In OnePasswordConfigurationProvider.Load():
  - Scan all configuration keys/values from previously loaded sources
  - Identify values starting with "op://" (FR-011)
  - Collect into list for batch retrieval
  - Log at INFO: "Found {count} op:// URIs in configuration"
  - **Files**: `src/OnePassword.Configuration/OnePasswordConfigurationProvider.cs`

- [ ] T046 [US2] Implement Load() method: Validate URIs
  - In Load() after collecting URIs:
  - Validate all op:// URIs using SecretReference.TryParse
  - If any malformed: throw MalformedUriException with configuration key and reason (fail fast per FR-028, FR-031)
  - Log at WARN: "Validating {count} op:// URIs"
  - **Files**: `src/OnePassword.Configuration/OnePasswordConfigurationProvider.cs`

- [ ] T047 [US2] Implement Load() method: Batch retrieve and cache secrets
  - In Load() after validation:
  - Call OnePasswordClient.GetSecretsAsync with all URIs
  - Cache all resolved secrets in ConcurrentDictionary
  - Replace configuration values: Set(key, resolvedSecret) for each
  - Handle errors: Any failure in GetSecretsAsync fails configuration build (fail-fast per FR-027, FR-030)
  - Log at INFO: "Successfully resolved and cached {count} secrets"
  - **Files**: `src/OnePassword.Configuration/OnePasswordConfigurationProvider.cs`

- [ ] T048 [US2] Implement OnePasswordConfigurationSource
  - Create: src/OnePassword.Configuration/OnePasswordConfigurationSource.cs
  - Implement IConfigurationSource
  - Properties: ConnectServer, Token (per contracts/)
  - Build(IConfigurationBuilder): Create and return OnePasswordConfigurationProvider instance
  - **Files**: `src/OnePassword.Configuration/OnePasswordConfigurationSource.cs`

- [ ] T049 [US2] Implement ConfigurationBuilderExtensions.AddOnePassword()
  - Create: src/OnePassword.Configuration/ConfigurationBuilderExtensions.cs
  - Implement 3 overloads per contracts/ConfigurationBuilderExtensions.cs:
    1. AddOnePassword(): Read credentials from existing config (OnePassword:ConnectServer, OnePassword:Token)
    2. AddOnePassword(connectServer, token): Explicit credentials
    3. AddOnePassword(Action<OnePasswordConfigurationSource>): Configure via action
  - Validate credentials present and valid (HTTPS URL)
  - Add OnePasswordConfigurationSource to builder
  - Return builder for chaining
  - **Files**: `src/OnePassword.Configuration/ConfigurationBuilderExtensions.cs`

- [ ] T050 [P] [US2] Unit test: OnePasswordConfigurationProvider Load() scanning
  - Create: tests/OnePassword.Configuration.Tests/ProviderLoadTests.cs
  - Test: Scan identifies all op:// URIs in configuration
  - Test: Non-op:// values are not collected for resolution
  - Test: Nested configuration keys scanned correctly (e.g., "Database:Password")
  - **Files**: `tests/OnePassword.Configuration.Tests/ProviderLoadTests.cs`

- [ ] T051 [P] [US2] Unit test: URI validation during Load()
  - Test: Malformed URI → Load() throws MalformedUriException with config key
  - Test: All URIs valid → Load() proceeds without exception
  - Test: Error message includes expected format guidance
  - **Files**: `tests/OnePassword.Configuration.Tests/ProviderValidationTests.cs`

- [ ] T052 [P] [US2] Unit test: Secret caching and retrieval
  - Test: Load() caches all resolved secrets in ConcurrentDictionary
  - Test: TryGet(key) returns cached secret for resolved op:// URI
  - Test: TryGet(key) returns original value for non-op:// keys
  - Test: Secrets remain cached after Load() (immutable per FR-020)
  - **Files**: `tests/OnePassword.Configuration.Tests/ProviderCachingTests.cs`

- [ ] T053 [P] [US2] Unit test: ConfigurationBuilderExtensions overloads
  - Create: tests/OnePassword.Configuration.Tests/ExtensionMethodsTests.cs
  - Test: AddOnePassword() reads credentials from builder's existing config
  - Test: AddOnePassword(server, token) uses explicit credentials
  - Test: AddOnePassword(configureOptions) applies action to source
  - Test: Invalid credentials (HTTP URL) → throws ArgumentException
  - Test: Missing credentials → throws InvalidOperationException
  - **Files**: `tests/OnePassword.Configuration.Tests/ExtensionMethodsTests.cs`

- [ ] T054 [US2] Integration test: Configuration integration end-to-end
  - Create: tests/OnePassword.Integration.Tests/ConfigurationIntegrationTests.cs
  - Set up: Create appsettings.json with op:// URIs
  - Build configuration with AddOnePassword()
  - Test: Acceptance scenario 1: op:// URI → resolved to actual secret
  - Test: Acceptance scenario 2: Multiple URIs → all resolved (verify batch call)
  - Test: Acceptance scenario 3: Non-op:// value → unchanged
  - Test: Acceptance scenario 4: Invalid URI → error with config key
  - Test: Acceptance scenario 5: Secrets resolved after other sources, before seal
  - **Files**: `tests/OnePassword.Integration.Tests/ConfigurationIntegrationTests.cs`

- [ ] T055 [US2] Create NuGet package metadata for OnePassword.Configuration
  - Edit: src/OnePassword.Configuration/OnePassword.Configuration.csproj
  - Add package metadata (similar to OnePassword.Sdk)
  - Add project reference: OnePassword.Sdk
  - Version: 1.0.0-beta1
  - **Files**: `src/OnePassword.Configuration/OnePassword.Configuration.csproj`

**Parallel Execution (Phase 4)**:
- T040 blocks T041, T042 (SecretReference needed)
- After T040: T041 (test) and T042 (implementation) in parallel
- After T042: T044-T047 (provider implementation, sequential due to dependencies)
- After T048: T049 (extensions)
- After T049: T050, T051, T052, T053 (all unit tests in parallel)
- T054 runs after all implementation complete

**Independent Test Validation**: T054 validates all acceptance scenarios for User Story 2

---

## Phase 5: User Story 3 - Environment Variable Override (P3)

**Story Goal**: Validate environment variables take precedence over 1Password secrets

**Why this priority**: Supports testing, CI/CD, and local development scenarios.

**Dependencies**: Requires User Story 2 complete (configuration provider)

**Independent Test Criteria** (from spec.md):
- ✅ Set environment variable with same key as op:// config
- ✅ Build configuration with both sources
- ✅ Verify environment variable value used (not 1Password secret)

**Acceptance Scenarios** (from spec.md User Story 3):
1. Env var + op:// URI for same key → env var wins
2. Env vars loaded before 1Password provider → env vars take precedence
3. No env var for op:// key → 1Password secret used

### Tasks

- [ ] T056 [P] [US3] Unit test: Configuration precedence with environment variables
  - Create: tests/OnePassword.Configuration.Tests/PrecedenceTests.cs
  - Test: Environment variable set for config key → overrides op:// URI resolution
  - Test: Environment variable NOT set → op:// URI resolved normally
  - Test: AddEnvironmentVariables() before AddOnePassword() → env vars take precedence
  - Test: Verify OnePasswordClient.GetSecretsAsync NOT called for overridden keys (optimization)
  - **Files**: `tests/OnePassword.Configuration.Tests/PrecedenceTests.cs`

- [ ] T057 [US3] Update OnePasswordConfigurationProvider to respect precedence
  - In Load() method:
  - Check if key already has value from higher-precedence source (e.g., env var)
  - Skip resolution for keys already set (per FR-024)
  - Only resolve op:// URIs if no higher-precedence value exists
  - Log at INFO: "Skipped {count} secrets overridden by higher-precedence sources"
  - **Files**: `src/OnePassword.Configuration/OnePasswordConfigurationProvider.cs`

- [ ] T058 [US3] Integration test: Environment variable override scenarios
  - Create: tests/OnePassword.Integration.Tests/EnvironmentOverrideTests.cs
  - Test: Acceptance scenario 1: Set env var "Database__Password" → overrides "op://..." value
  - Test: Acceptance scenario 2: AddEnvironmentVariables() before AddOnePassword() → env precedence
  - Test: Acceptance scenario 3: No env var → op:// secret used as expected
  - Test: Verify configuration builder order determines precedence (standard .NET behavior)
  - **Files**: `tests/OnePassword.Integration.Tests/EnvironmentOverrideTests.cs`

- [ ] T059 [US3] Update quickstart.md with environment variable override examples
  - Add section: "Development vs. Production Configuration"
  - Show example: Override op:// URIs with env vars for local dev
  - Document builder order: AddEnvironmentVariables() before AddOnePassword()
  - **Files**: `specs/001-onepassword-sdk/quickstart.md` (already exists, update)

**Parallel Execution (Phase 5)**:
- T056 and T057 can run in parallel (test and implementation)
- T058 runs after T057 complete
- T059 (documentation) can run in parallel with tests

**Independent Test Validation**: T058 validates all acceptance scenarios for User Story 3

---

## Phase 6: Polish & Cross-Cutting Concerns

**Goal**: Complete observability, documentation, and production-readiness

**Prerequisites**: All user stories complete

### Tasks

- [ ] T060 [P] Implement comprehensive logging throughout SDK
  - Review all OnePasswordClient methods: Add INFO logs for success, WARN for retries, ERROR for failures
  - Implement correlation ID tracking using AsyncLocal<string> or System.Diagnostics.Activity (FR-044)
  - Include correlation ID in all structured log entries as "{CorrelationId}" property
  - Generate correlation ID on client initialization; propagate through async call chain
  - Ensure no secret values logged (Field.Value, Token excluded)
  - Sanitize all error logs (no partial secrets per FR-038)
  - Per FR-039 through FR-044
  - **Files**: `src/OnePassword.Sdk/Client/OnePasswordClient.cs`, `src/OnePassword.Sdk/Internal/CorrelationContext.cs` (new), all exception classes

- [ ] T061 [P] Implement comprehensive logging in configuration provider
  - Add logging to OnePasswordConfigurationProvider: Load() lifecycle events
  - Log: URI collection, validation, batch retrieval, caching
  - Include operation timing (e.g., "Batch retrieval completed in {ms}ms")
  - **Files**: `src/OnePassword.Configuration/OnePasswordConfigurationProvider.cs`

- [ ] T062 [P] Add XML documentation comments to all public APIs
  - Review all public classes, methods, properties
  - Ensure XML comments exist (already in contracts, verify implementation matches)
  - Enable GenerateDocumentationFile in all project files
  - **Files**: All public API files in src/OnePassword.Sdk and src/OnePassword.Configuration

- [ ] T063 Create README.md at repository root
  - Overview: What is this SDK, key features
  - Quick Start: Installation, basic usage example (link to quickstart.md)
  - Documentation: Link to specs/001-onepassword-sdk/quickstart.md
  - Contributing: How to build, test, contribute
  - License: Specify license
  - **Files**: `README.md` (repository root)

- [ ] T064 [P] Create CHANGELOG.md
  - Document version 1.0.0-beta1:
    - Initial release
    - Core SDK (User Story 1)
    - Configuration provider (User Story 2)
    - Environment variable override (User Story 3)
  - **Files**: `CHANGELOG.md` (repository root)

- [ ] T065 [P] Add code coverage tooling
  - Add coverlet.collector to test projects (for code coverage)
  - Configure solution to generate coverage reports
  - Target: >80% coverage for core logic
  - **Files**: Test project .csproj files, coverage configuration

- [ ] T066 [P] Security review: Verify no secret leakage
  - Manual review: Search codebase for any logging of Token, Field.Value, secret strings
  - Verify Field.ToString() excludes Value
  - Verify exception messages sanitized (no partial secrets)
  - Verify ILogger calls exclude sensitive data
  - Document findings
  - **Files**: All source files (review only)

- [ ] T067 Create examples directory with sample projects
  - Create: examples/ProgrammaticAccess (console app using core SDK)
  - Create: examples/ConfigurationIntegration (console app with appsettings.json)
  - Create: examples/AspNetCoreIntegration (minimal ASP.NET Core app)
  - Each example: working code, README explaining use case
  - **Files**: `examples/*/` directories with sample code

- [ ] T068 [P] Performance benchmark: Startup overhead
  - Create: benchmarks/StartupBenchmark.cs (using BenchmarkDotNet)
  - Measure: Configuration build time with 20 op:// URIs
  - Target: <500ms (per SC-003 success criterion)
  - Document results
  - **Files**: `benchmarks/StartupBenchmark.cs`

- [ ] T069 [P] Performance benchmark: Batch retrieval
  - Measure: GetSecretsAsync with 100 unique URIs
  - Target: <10s (per constraint)
  - Verify retry and timeout policies don't exceed limits
  - **Files**: `benchmarks/BatchRetrievalBenchmark.cs`

- [ ] T070 Final integration test: All user stories together
  - Create: tests/OnePassword.Integration.Tests/FullSystemTests.cs
  - Test: ASP.NET Core app with multiple secrets, env var overrides, error scenarios
  - Simulate production-like usage
  - Verify all success criteria (SC-001 through SC-008)
  - **Files**: `tests/OnePassword.Integration.Tests/FullSystemTests.cs`

- [ ] T071 Update NuGet package metadata for release
  - Set Version to 1.0.0 (remove -beta1)
  - Add release notes
  - Verify all metadata fields (authors, license, tags, repository URL)
  - **Files**: src/OnePassword.Sdk/OnePassword.Sdk.csproj, src/OnePassword.Configuration/OnePassword.Configuration.csproj

**Parallel Execution (Phase 6)**:
- Group A: T060, T061, T062 (logging and docs in code)
- Group B: T063, T064, T065 (repo-level docs and tooling)
- T066 (security review) can run anytime
- Group C: T067, T068, T069 (examples and benchmarks)
- T070 runs after all implementation complete
- T071 (final packaging) runs last

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup)
    ↓
Phase 2 (Foundational)
    ↓
Phase 3 (User Story 1 - P1) ─┐
    ↓                        │
Phase 4 (User Story 2 - P2) ←┘ (depends on Phase 3)
    ↓
Phase 5 (User Story 3 - P3) ←─ (depends on Phase 4)
    ↓
Phase 6 (Polish)
```

### User Story Independence

- **User Story 1** is fully independent (core SDK standalone)
- **User Story 2** depends on User Story 1 (uses core SDK client)
- **User Story 3** depends on User Story 2 (validates configuration provider)

### MVP Delivery Path

**Minimum Viable Product**: Phase 1 → Phase 2 → Phase 3 only

Delivers complete, production-ready core SDK library for programmatic vault access.

---

## Parallel Execution Examples

### Phase 1 Parallelization
```bash
# After T001 completes:
# Parallel group: Create all projects simultaneously
dotnet new classlib -n OnePassword.Sdk &
dotnet new classlib -n OnePassword.Configuration &
dotnet new xunit -n OnePassword.Sdk.Tests &
dotnet new xunit -n OnePassword.Configuration.Tests &
dotnet new xunit -n OnePassword.Integration.Tests &
wait

# Then add dependencies in parallel:
# (Edit all .csproj files simultaneously)
```

### Phase 2 Parallelization
```bash
# All exception classes can be created in parallel:
# T012, T013, T014, T015, T016 → Independent files

# All model classes can be created in parallel:
# T017, T018, T019, T020, T021 → Independent files

# Tests run in parallel after implementation complete
```

### Phase 3 Parallelization
```bash
# After T026 (client constructor) completes:
# T027 (ListVaultsAsync) and T029 (ListItemsAsync) can run in parallel

# After implementation complete:
# T034, T035, T036, T037 → All unit test files independent, run in parallel
```

### Phase 4 Parallelization
```bash
# After T040 (SecretReference model):
# T041 (test) and T042 (GetSecretsAsync implementation) in parallel

# After T049 (extensions complete):
# T050, T051, T052, T053 → All unit test files in parallel
```

### Phase 6 Parallelization
```bash
# Documentation, examples, benchmarks largely independent:
# T063, T064, T067, T068, T069 can all run in parallel
```

---

## Task Validation Checklist

✅ **Format Compliance**:
- All tasks use `- [ ] [TaskID]` format
- Task IDs sequential (T001-T071)
- `[P]` marker on parallelizable tasks
- `[US1]`, `[US2]`, `[US3]` labels on user story tasks
- All tasks include file paths

✅ **Coverage**:
- All user stories from spec.md covered
- All entities from data-model.md implemented
- All contracts from contracts/ implemented
- All functional requirements (FR-001 through FR-044) mapped to tasks

✅ **Test Strategy**:
- Unit tests for all core logic (exceptions, models, parsing, caching)
- Integration tests for each user story's acceptance scenarios
- End-to-end tests validating independent test criteria

✅ **Independence**:
- Each user story phase is independently testable
- Acceptance scenarios validated in integration tests
- MVP scope clearly defined (Phase 3 only)

---

## Task Summary

**Total Tasks**: 71

**By Phase**:
- Phase 1 (Setup): 11 tasks
- Phase 2 (Foundational): 12 tasks
- Phase 3 (User Story 1 - P1): 16 tasks
- Phase 4 (User Story 2 - P2): 16 tasks
- Phase 5 (User Story 3 - P3): 4 tasks
- Phase 6 (Polish): 12 tasks

**By User Story**:
- User Story 1 tasks: 16 (T024-T039)
- User Story 2 tasks: 16 (T040-T055)
- User Story 3 tasks: 4 (T056-T059)

**Parallelizable Tasks**: 43 tasks marked with `[P]`

**Test Tasks**: 22 tasks (unit tests, integration tests, benchmarks)

**MVP Scope**: 39 tasks (Phase 1 + Phase 2 + Phase 3)

---

## Success Criteria Validation

Each user story phase includes tasks that validate the specification's success criteria:

**User Story 1** validates:
- SC-001: <10 lines of code for integration (T038)
- SC-003: <500ms startup for 20 secrets (T068 benchmark)
- SC-005: Clear error messages (T022, T034-T037)
- SC-007: Network failure handling (T037)
- SC-008: No secret logging (T066 security review)

**User Story 2** validates:
- SC-002: Automatic resolution without custom code (T054)
- SC-004: 95% first-attempt success (quickstart.md clarity, T054)

**User Story 3** validates:
- SC-006: Env vars override 100% (T058)

**Cross-cutting** (Phase 6):
- SC-005: Error diagnosis <5 min (T063 README, T067 examples)
- SC-008: Zero secret exposure (T060, T061, T066)

---

**Generated**: 2025-11-18
**Ready for Implementation**: Yes
**Next Step**: Begin with Phase 1 (T001)
