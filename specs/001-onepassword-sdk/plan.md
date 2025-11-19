# Implementation Plan: 1Password .NET SDK with Configuration Integration

**Branch**: `001-onepassword-sdk` | **Date**: 2025-11-18 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-onepassword-sdk/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Build a .NET SDK library for programmatic access to 1Password vaults via the Connect API, with seamless integration into the Microsoft.Extensions.Configuration system. The SDK will enable developers to reference secrets using `op://` URIs in configuration files (appsettings.json) and automatically resolve them to actual secret values during application startup. The implementation will port the official 1Password Connect SDK patterns from JavaScript/Python to .NET, providing a library-first architecture with minimal dependencies and comprehensive observability.

## Technical Context

**Language/Version**: C# / .NET 8.0 
**Primary Dependencies**:
- Microsoft.Extensions.Configuration.Abstractions (configuration provider integration)
- Microsoft.Extensions.Logging.Abstractions (structured logging)
- System.Net.Http (1Password Connect API client)
- JSON serialization library: System.Text.Json 
- HTTP client resilience patterns: Polly for retry/timeout

**Storage**: N/A (in-memory secret caching only, no persistent storage)
**Testing**: xUnit with Moq for mocking and autofixture for test data generation
**Target Platform**: Cross-platform .NET (Linux, Windows, macOS)
**Project Type**: Single library project with multiple packages (core SDK + configuration provider extension)
**Performance Goals**:
- <500ms application startup overhead for resolving up to 20 secrets
- <10 seconds total timeout for batch retrieval of up to 100 secrets
- Minimal memory footprint (in-memory cache limited to resolved secrets only, max 100MB total)

**Constraints**:
- Maximum 100 secrets per batch retrieval operation
- Maximum 1MB per individual secret value
- 10-second total timeout for batch secret retrieval (excluding retries)
- 3 retry attempts with exponential backoff for transient network failures

**Scale/Scope**:
- Support for typical .NET applications requiring 1-100 secrets
- Library designed for integration into ASP.NET Core, console apps, background services
- ~2,000-5,000 lines of code estimated (core SDK + configuration provider + tests)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Security-First Development ✅ PASS

**Compliance**:
- ✅ Spec explicitly prohibits logging, caching, or persisting secrets in plaintext (FR-031, FR-035)
- ✅ HTTPS required for all 1Password API interactions (FR-037)
- ✅ Input validation mandated for op:// URI syntax (FR-028, FR-031)
- ✅ Error message sanitization to prevent secret leakage (FR-038)
- ✅ Token handling security specified (FR-036)
- ✅ Comprehensive error handling for security boundaries (authentication, authorization)

**Assessment**: No violations. Security requirements align perfectly with constitution.

### II. Library-First Architecture ✅ PASS

**Compliance**:
- ✅ Framework-agnostic core SDK design (can be used standalone)
- ✅ Configuration provider as optional extension (clean separation)
- ✅ Follows official 1Password SDK patterns (JavaScript/Python) per constitution requirement
- ✅ Minimal dependencies (only Microsoft.Extensions abstractions, System.Net.Http)
- ✅ Async API variants planned (requirement for I/O operations)
- ✅ No coupling to specific UI frameworks, ORMs, or infrastructure

**Assessment**: No violations. Library-first approach is core to the design.

### III. API Simplicity & Developer Experience ✅ PASS

**Compliance**:
- ✅ Success criterion SC-001: <10 lines of code for integration
- ✅ Success criterion SC-004: 95% first-attempt success without docs
- ✅ Success criterion SC-005: Error diagnosis within 5 minutes
- ✅ Clear error messages with context mandated (FR-029, FR-032)
- ✅ Sensible defaults (environment variables override appsettings.json)
- ✅ Pit of success: automatic op:// URI resolution without manual code

**Assessment**: No violations. DX is a primary design goal.

### IV. Test-Driven Development (Recommended) ⚠️ DEFERRED

**Status**: Tests MUST exist before merge, but TDD during implementation is RECOMMENDED (not required).

**Plan**: Comprehensive test coverage specified in spec (User Stories have Independent Test sections, Edge Cases defined). Tests will be written during implementation phase.

### V. Observability & Diagnostics ✅ PASS

**Compliance**:
- ✅ Microsoft.Extensions.Logging integration mandated (FR-039)
- ✅ Structured logging at INFO/WARN/ERROR levels specified (FR-040, FR-041, FR-042)
- ✅ Correlation identifiers required (FR-044)
- ✅ Log sanitization to prevent secret disclosure (FR-043)
- ✅ Clear diagnostic context in all error messages (FR-029)

**Assessment**: No violations. Observability is fully specified.

### Development Standards ✅ PASS

- ✅ Security testing requirements covered in spec (Edge Cases, Error Handling)
- ✅ Public API testing via User Story acceptance scenarios
- ✅ Documentation requirements implicit (quickstart.md will be generated in Phase 1)

### **GATE RESULT: PASS** - Proceed to Phase 0 Research

---

## Constitution Check (Post-Design Re-evaluation)

*Re-checked after Phase 1 design completion*

### I. Security-First Development ✅ PASS

**Post-Design Compliance**:
- ✅ All exception messages sanitized (see contracts/Exceptions.cs - no secret values in messages)
- ✅ API contracts enforce HTTPS (OnePasswordClientOptions validation)
- ✅ Input validation designed (SecretReference.TryParse with fail-fast)
- ✅ Logging design excludes secrets (Field.ToString() override, ILogger integration sanitized)
- ✅ Token handling secure (marked as sensitive in options, excluded from logs)

**Assessment**: Design artifacts demonstrate security-first approach throughout.

### II. Library-First Architecture ✅ PASS

**Post-Design Compliance**:
- ✅ Clean separation: `OnePassword.Sdk` (core) + `OnePassword.Configuration` (optional extension)
- ✅ No framework coupling in core SDK (only System.* and Microsoft.Extensions.Logging.Abstractions)
- ✅ Async APIs throughout (all I/O operations)
- ✅ Follows official SDK patterns (research.md validates JavaScript/Python alignment)
- ✅ Minimal dependencies confirmed (research.md: System.Text.Json, Polly justified)

**Assessment**: Multi-package structure perfectly implements library-first principle.

### III. API Simplicity & Developer Experience ✅ PASS

**Post-Design Compliance**:
- ✅ Fluent API designed (`builder.Configuration.AddOnePassword()` is 1 line)
- ✅ XML documentation in all contract files
- ✅ Quickstart.md provides code examples for all common scenarios
- ✅ Error messages designed with context (see Exceptions.cs - includes vault/item/field names)
- ✅ Sensible defaults (environment variables auto-override, 3 retry attempts, 10s timeout)
- ✅ Pit of success: op:// URIs auto-resolve without manual code

**Assessment**: API design achieves SC-001 (<10 lines), SC-004 (95% first-attempt success).

### IV. Test-Driven Development (Recommended) ⚠️ DEFERRED TO IMPLEMENTATION

**Status**: Design complete, tests will be written during implementation.

**Test Strategy Designed**:
- Unit tests for URI parsing, validation, caching logic
- Integration tests with mocked 1Password API (using Moq)
- Contract tests for API compatibility
- Scenario coverage from spec User Stories

### V. Observability & Diagnostics ✅ PASS

**Post-Design Compliance**:
- ✅ ILogger integration designed (constructor injection in client)
- ✅ Structured logging levels defined (INFO/WARN/ERROR per research.md)
- ✅ Correlation IDs planned (async context tracking)
- ✅ Log sanitization designed (Field.ToString() override excludes Value)
- ✅ Diagnostic metadata designed (operation timing, retry counts in logs)

**Assessment**: Observability is first-class concern in API design.

### Development Standards ✅ PASS

- ✅ Security: All exceptions designed with context, sanitization enforced
- ✅ Testing: Acceptance scenarios defined in spec, test structure in plan
- ✅ Documentation: quickstart.md generated, XML comments in contracts

### **GATE RESULT: PASS** - Design aligns with all constitution principles

**Changes Since Initial Check**:
- Research completed: System.Text.Json, Polly, xUnit selected (dependencies justified)
- Contracts designed: All APIs follow .NET conventions, XML documented
- Data model designed: Immutable entities, security annotations
- Quickstart guide: <10 lines for integration (SC-001 validated)

**No violations introduced during design phase. Ready for Phase 2 (Task Generation).**

---

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── OnePassword.Sdk/                     # Core SDK library (NuGet package)
│   ├── Client/                          # 1Password Connect API client
│   │   ├── IOnePasswordClient.cs
│   │   ├── OnePasswordClient.cs
│   │   └── OnePasswordClientOptions.cs
│   ├── Models/                          # Domain entities
│   │   ├── Vault.cs
│   │   ├── Item.cs
│   │   ├── Field.cs
│   │   └── SecretReference.cs
│   ├── Exceptions/                      # Custom exceptions
│   │   ├── OnePasswordException.cs
│   │   ├── AuthenticationException.cs
│   │   ├── VaultNotFoundException.cs
│   │   └── [other specific exceptions]
│   └── Internal/                        # Internal utilities
│       ├── UriParser.cs
│       ├── RetryPolicy.cs
│       └── SecretCache.cs
│
├── OnePassword.Configuration/           # Configuration provider extension (NuGet package)
│   ├── OnePasswordConfigurationProvider.cs
│   ├── OnePasswordConfigurationSource.cs
│   ├── ConfigurationBuilderExtensions.cs
│   └── Internal/
│       └── SecretResolver.cs
│
└── OnePassword.Abstractions/            # Shared interfaces (optional, for DI scenarios)
    └── IOnePasswordClient.cs

tests/
├── OnePassword.Sdk.Tests/               # Unit tests for core SDK
│   ├── Client/
│   ├── Models/
│   └── Internal/
│
├── OnePassword.Configuration.Tests/     # Unit tests for configuration provider
│   ├── ProviderTests.cs
│   └── ResolverTests.cs
│
└── OnePassword.Integration.Tests/       # Integration tests (mocked API)
    ├── EndToEndTests.cs
    └── ConfigurationIntegrationTests.cs
```

**Structure Decision**: Multi-package library structure aligns with Library-First Architecture principle. Core SDK (`OnePassword.Sdk`) is independently usable for programmatic access. Configuration provider (`OnePassword.Configuration`) is an optional extension for Microsoft.Extensions.Configuration integration. This allows consumers to use only what they need while maintaining clean separation of concerns.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No violations requiring justification. All design choices align with constitution principles.
