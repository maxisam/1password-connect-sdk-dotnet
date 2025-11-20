<!--
  ============================================================================
  SYNC IMPACT REPORT
  ============================================================================

  Version Change: [NONE] → 1.0.0

  Modified Principles:
  - NEW: I. Security-First Development
  - NEW: II. Library-First Architecture
  - NEW: III. API Simplicity & Developer Experience
  - NEW: IV. Test-Driven Development (Recommended)
  - NEW: V. Observability & Diagnostics

  Added Sections:
  - Core Principles (5 principles defined)
  - Development Standards (security, testing, documentation requirements)
  - Governance (amendment procedure, versioning policy, compliance)

  Removed Sections:
  - None (initial constitution)

  Templates Requiring Updates:
  - ✅ plan-template.md: Constitution Check section aligns with principles
  - ✅ spec-template.md: Requirements structure compatible with constitution
  - ✅ tasks-template.md: Task organization supports TDD and security principles
  - ✅ checklist-template.md: Generic structure, no conflicts
  - ✅ agent-file-template.md: Not reviewed (agent-specific)

  Follow-up TODOs:
  - None

  ============================================================================
-->

# dotnet-1password Constitution

## Core Principles

### I. Security-First Development

Security is the paramount concern for this library. All code handling secrets, credentials, or
1Password integration MUST follow secure coding practices. This principle is NON-NEGOTIABLE.

**Requirements**:
- MUST never log, cache, or persist secrets in plaintext
- MUST use secure communication channels (HTTPS, encrypted connections) for all 1Password API calls
- MUST validate all inputs and sanitize outputs to prevent injection attacks
- MUST handle authentication tokens and session data with appropriate security controls
- MUST implement proper error handling that does not leak sensitive information
- MUST follow OWASP Top 10 security guidelines
- MUST undergo security review for any changes to authentication, authorization, or secret handling

**Rationale**: This library handles highly sensitive data (passwords, secrets, credentials). A
single security vulnerability could compromise user vaults and credentials. Security cannot be
retrofitted; it must be designed in from the start.

### II. Library-First Architecture

Every feature MUST be built as a reusable library component with clear separation of concerns.
The library MUST be independently usable, testable, and documented without external dependencies
where possible.

The library MUST follow the official 1Password SDK documentation for python where applicable from https://developer.1password.com/docs/sdks.

**Requirements**:
- MUST design APIs that are framework-agnostic and can be consumed by any .NET core application
- MUST provide clear interfaces and abstractions for core functionality
- MUST minimize external dependencies; justify each dependency addition
- MUST ensure library components are independently testable without infrastructure dependencies
- MUST provide both synchronous and asynchronous API variants where I/O operations occur
- MUST NOT couple library logic to specific UI frameworks, ORMs, or infrastructure choices
- MUST follow semantic versioning for all library releases

**Rationale**: Users will integrate this library into diverse .NET applications (ASP.NET,
console apps, background services, etc.). A library-first approach ensures maximum reusability
and flexibility while maintaining clear boundaries and testability.

### III. API Simplicity & Developer Experience

Public APIs MUST be intuitive, well-documented, and follow .NET conventions. Complexity MUST
be hidden behind simple, composable interfaces. The library should feel natural to .NET developers. They should be similar to the official 1Password SDK for python.

**Requirements**:
- MUST follow .NET naming conventions and design guidelines
- MUST provide XML documentation comments for all public APIs
- MUST include code examples in documentation for common use cases
- MUST design APIs that guide developers toward correct usage (pit of success)
- MUST provide helpful error messages with actionable guidance
- MUST minimize required configuration; provide sensible defaults
- MUST expose advanced features through optional parameters or extension points
- MUST provide IntelliSense-friendly APIs with descriptive parameter names

**Rationale**: Developer experience directly impacts adoption and correct usage. Complex or
poorly documented APIs lead to misuse, bugs, and security vulnerabilities. A simple,
well-documented API reduces cognitive load and prevents errors.

### IV. Test-Driven Development (Recommended)

Testing is essential for quality and security. Test-Driven Development is RECOMMENDED for new
features. At minimum, comprehensive tests MUST exist before code is merged to main.

**Requirements**:
- SHOULD write tests before implementation (TDD approach) for new features
- MUST have failing tests before fixing bugs (to prove the fix works)
- MUST include unit tests for all business logic and algorithms
- MUST include integration tests for 1Password API interactions (with mocking/stubs)
- MUST include contract tests to verify API compatibility across versions
- MUST achieve meaningful test coverage (not just line coverage, but scenario coverage)
- MUST test error conditions, edge cases, and security boundaries
- MAY defer tests during rapid prototyping, but tests MUST be added before PR approval

**Rationale**: TDD encourages better design and catches issues early. However, strict TDD can
slow exploration and prototyping. This principle balances quality with pragmatism: tests are
mandatory, but strict test-first is recommended rather than required.

### V. Observability & Diagnostics

The library MUST provide comprehensive logging, diagnostics, and debugging capabilities to help
developers troubleshoot integration issues without exposing sensitive data.

**Requirements**:
- MUST use structured logging (Microsoft.Extensions.Logging or compatible)
- MUST log all API calls, errors, and significant operations (without logging secrets)
- MUST provide clear correlation IDs for tracking operations across async boundaries
- MUST include diagnostic metadata (versions, configuration, timing) in logs
- MUST sanitize all log output to prevent accidental secret disclosure
- MUST provide debugging helpers and diagnostic tools for troubleshooting
- MUST include telemetry hooks for consumers to integrate with their monitoring systems

**Rationale**: Integration issues are inevitable. Without proper observability, developers
waste time debugging black-box failures. Structured logging and diagnostics enable rapid
troubleshooting while maintaining security through proper sanitization.

## Development Standards

### Security Standards

- Security vulnerabilities are P0 bugs and MUST be addressed immediately
- All PRs touching authentication, authorization, or secret handling MUST have security review
- Dependency updates addressing security issues MUST be applied within 48 hours
- Security testing MUST include: input validation, injection prevention, secret handling,
  authentication flows

### Testing Standards

- All public APIs MUST have integration tests
- All business logic MUST have unit tests
- Test names MUST clearly describe the scenario being tested
- Tests MUST be independent and not rely on execution order
- Integration tests MUST use mocks/stubs for external dependencies (1Password API)
- Performance-critical paths SHOULD include benchmark tests

### Documentation Standards

- All public APIs MUST have XML documentation comments
- All significant features MUST have usage examples
- Breaking changes MUST be documented in release notes
- README MUST include quickstart guide and common scenarios
- Migration guides MUST be provided for breaking changes

## Governance

### Amendment Procedure

This constitution governs all development for dotnet-1password. Amendments require:

1. Documented proposal with rationale for change
2. Review and approval from project maintainers
3. Migration plan if changes affect existing workflows or standards
4. Version bump according to semantic versioning rules

### Versioning Policy

Constitution versions follow semantic versioning (MAJOR.MINOR.PATCH):

- **MAJOR**: Backward-incompatible governance changes (e.g., removing a core principle,
  fundamentally changing development approach)
- **MINOR**: New principles added, material expansion of existing guidance, new mandatory
  sections
- **PATCH**: Clarifications, wording improvements, typo fixes, non-semantic refinements

### Compliance Review

- All PRs MUST verify compliance with applicable principles
- Complexity and deviations from principles MUST be explicitly justified
- Constitution violations without justification will block PR approval
- Reviewers MUST validate security, testing, and documentation standards

**Version**: 1.0.0 | **Ratified**: 2025-11-17 | **Last Amended**: 2025-11-17
