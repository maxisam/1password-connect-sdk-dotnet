# Feature Specification: 1Password .NET SDK with Configuration Integration

**Feature Branch**: `001-onepassword-sdk`
**Created**: 2025-11-17
**Status**: Draft
**Input**: User description: "Build SDK for .NET applications to access 1Password vaults and also provide helper function to integrate with configuration builder, so when there is a configuration with value starting with "op://" like op://<vault>/<item>/<field>, it will replace the value with the real secret from 1password. The process should happen in the end of configuration building so it can go through every configuration value, and it can only be override by environment variables. User should provide necessary authentication info in "appsettings.json" or Environment variable to access 1password vaults."

## Clarifications

### Session 2025-11-17

- Q: How should the SDK handle transient network failures when retrieving secrets during configuration building? → A: Retry up to 3 times with exponential backoff (total ~7 seconds), then fail with clear error
- Q: Should the SDK support optional secrets (with default values) or should all op:// URIs be treated as required? → A: All op:// URIs are required - failure to resolve any secret fails configuration build
- Q: Should secret retrieval operations during configuration building happen serially or in parallel? → A: Batch retrieval - collect all op:// URIs and retrieve all secrets in a single API call
- Q: What should the configuration structure look like for 1Password authentication credentials? → A: Support both appsettings.json (OnePassword:ConnectServer, OnePassword:Token) and environment variables (OnePassword__ConnectServer, OnePassword__Token), with environment variables taking precedence
- Q: When should malformed op:// URIs be detected and how should they be handled? → A: Validate URI syntax during configuration building, fail immediately if malformed (before attempting retrieval)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Programmatic Vault Access (Priority: P1)

A .NET developer needs to retrieve secrets from 1Password vaults through the [connect server ](https://developer.1password.com/docs/connect/) programmatically within their application code. They want a simple, type-safe API to authenticate with 1Password and retrieve specific secrets without manual copy-paste or hardcoding credentials. 

The APIs should be similar to the official 1Password connect SDK for python or javascript. In fact, porting that SDK to .NET would be ideal. 

Here is javascript sdk repository for reference: https://github.com/1Password/connect-sdk-js

for python sdk repository: https://github.com/1Password/connect-sdk-python

**Why this priority**: This is the foundation of the SDK. Without the ability to programmatically access vaults and retrieve secrets, no other functionality is possible. This delivers immediate value as a standalone library for secure secret management.

**Independent Test**: Can be fully tested by initializing the SDK with authentication credentials, connecting to a 1Password vault, retrieving a specific item by name, and extracting a secret field value. Success means the secret is retrieved and matches the expected value in 1Password.

**Acceptance Scenarios**:

1. **Given** a developer has valid 1Password authentication credentials, **When** they initialize the SDK and request a secret from a specific vault and item, **Then** the SDK returns the correct secret value through self-hosted 1Password Connect server.
2. **Given** a developer requests a non-existent vault or item, **When** they attempt to retrieve the secret, **Then** the SDK throws a clear error indicating what was not found
3. **Given** a developer has invalid authentication credentials, **When** they initialize the SDK, **Then** the SDK fails authentication and provides a clear error message
4. **Given** multiple secrets exist in a single item, **When** a developer requests a specific secret field by name, **Then** only that specific field value is returned

---

### User Story 2 - Configuration Builder Integration (Priority: P2)

A .NET developer building an ASP.NET application wants to store sensitive configuration values (database passwords, API keys) in 1Password and reference them in "appsettings.json" using "op://" URIs. The application should automatically resolve these URIs to actual secrets during startup without requiring manual code for each secret.

URI format: `op://<vault-name>/<item-name>/<field-name>` , following the official 1Password secret reference syntax (https://developer.1password.com/docs/cli/secret-reference-syntax

**Why this priority**: This is the key differentiator of this SDK. It enables the "pit of success" pattern where developers can manage secrets in 1Password and seamlessly integrate them into the standard .NET configuration system. This delivers significant value by eliminating boilerplate secret-loading code.

**Independent Test**: Can be fully tested by creating a configuration file (appsettings.json) with op:// URI values, adding the 1Password configuration provider to the configuration builder, and verifying that when the application reads configuration values, it receives the actual secrets from 1Password instead of the op:// URIs.

**Acceptance Scenarios**:

1. **Given** an appsettings.json file contains a value like `op://prod/database/password`, **When** the application configures the 1Password provider and builds configuration, **Then** reading the configuration key returns the actual password from 1Password
2. **Given** multiple configuration values reference different 1Password secrets, **When** configuration is built, **Then** all op:// URIs are resolved to their respective secrets in a single batch API call
3. **Given** a configuration value does not start with "op://", **When** configuration is built with the 1Password provider, **Then** the value is left unchanged
4. **Given** an op:// URI references a non-existent vault, item, or secret field, **When** configuration is built, **Then** the provider throws a clear error identifying which reference failed
5. **Given** the 1Password configuration provider is added to the configuration builder, **When** configuration is built, **Then** secret resolution happens after all other configuration sources are loaded but before the final configuration is sealed

---

### User Story 3 - Environment Variable Override (Priority: P3)

A .NET developer needs to override 1Password secrets with environment variables for different deployment environments (local development, CI/CD, testing) without modifying configuration files. Environment variables should take precedence over op:// resolved secrets.

**Why this priority**: This provides flexibility for different deployment scenarios. While not critical for basic functionality, it's essential for CI/CD pipelines, local development, and testing environments where developers need to override production secrets with test values.

**Independent Test**: Can be fully tested by setting an environment variable with the same key as a configuration value that has an op:// URI, building configuration with both environment variables and 1Password provider, and verifying that the environment variable value is used instead of the 1Password secret.

**Acceptance Scenarios**:

1. **Given** a configuration key "DatabasePassword" has value `op://prod/database/password` and an environment variable "DatabasePassword=local-test-password" exists, **When** configuration is built with both sources, **Then** the environment variable value "local-test-password" is used
2. **Given** environment variables are configured to load before the 1Password provider, **When** configuration is built, **Then** environment variables take precedence over op:// resolved secrets
3. **Given** no environment variable exists for a configuration key with an op:// URI, **When** configuration is built, **Then** the 1Password secret is used as expected

---

### Edge Cases

- **Network connectivity loss during secret retrieval**: SDK will retry up to 3 times with exponential backoff (total maximum ~7 seconds), then fail with a clear error message indicating network failure and which secret could not be retrieved
- **Concurrent secret retrieval**: Configuration provider collects all op:// URIs from configuration and retrieves all secrets in a single batch API call to 1Password (no concurrent individual requests)
- **Same op:// URI referenced multiple times**: URI is only fetched once in the batch call; resolved value is reused for all references
- **Malformed op:// URIs (invalid syntax, missing components)**: SDK validates all op:// URI syntax during configuration building and fails immediately with a clear error indicating which URI is malformed and why, before attempting any secret retrieval
- **1Password item exists but requested secret field name doesn't exist**: SDK throws a clear error during secret retrieval identifying the vault, item, and missing field name
- What happens when authentication token expiration during long-running applications?
- What happens when vault permissions change after the application starts?
- How are circular references in configuration handled if they involve op:// URIs?

## Requirements *(mandatory)*

### Functional Requirements

**Core SDK Functionality:**

- **FR-001**: SDK MUST provide an API to authenticate with 1Password using service account tokens
- **FR-002**: SDK MUST provide an API to list available vaults for the authenticated account
- **FR-003**: SDK MUST provide an API to retrieve a specific item from a vault by item name or ID
- **FR-004**: SDK MUST provide an API to extract a specific secret field value from an item
- **FR-005**: SDK MUST support authentication via 1Password Connect server (Connect server URL with token)
- **FR-006**: SDK MUST read authentication configuration from both appsettings.json (OnePassword:ConnectServer, OnePassword:Token) and environment variables (OnePassword__ConnectServer, OnePassword__Token)
- **FR-007**: SDK MUST give precedence to environment variables over appsettings.json for authentication configuration when both are present
- **FR-008**: SDK MUST handle authentication errors and provide clear, actionable error messages
- **FR-009**: SDK MUST validate authentication credentials before attempting vault operations

**Configuration Integration:**

- **FR-010**: SDK MUST provide a configuration provider that integrates with Microsoft.Extensions.Configuration
- **FR-011**: Configuration provider MUST recognize configuration values starting with "op://" as 1Password secret references
- **FR-012**: Configuration provider MUST parse op:// URIs in the format `op://<vault-name>/<item-name>/<field-name>` per official 1Password secret reference syntax (https://developer.1password.com/docs/cli/secret-reference-syntax/)
- **FR-013**: Configuration provider MUST resolve op:// URIs to actual secret values from 1Password during configuration building
- **FR-014**: Configuration provider MUST process all configuration keys and values to identify op:// URIs (not just top-level values)
- **FR-015**: Configuration provider MUST execute secret resolution after all other configuration sources are loaded
- **FR-016**: Configuration provider MUST execute secret resolution before the configuration is finalized/sealed
- **FR-017**: Configuration provider MUST NOT modify configuration values that don't start with "op://"
- **FR-018**: Configuration provider MUST collect all op:// URIs and retrieve secrets using a single batch API call to 1Password (not individual calls per secret)
- **FR-019**: Configuration provider MUST deduplicate op:// URIs so each unique secret is fetched only once, even if referenced multiple times

**Environment Variable Override:**

- **FR-020**: Configuration provider MUST respect the standard .NET configuration precedence order
- **FR-021**: Environment variables added to the configuration builder MUST override op:// resolved secrets when both are present for the same key
- **FR-022**: Configuration provider MUST only resolve op:// URIs if no higher-precedence configuration source has already provided a value for that key

**Error Handling:**

- **FR-023**: SDK MUST throw specific exceptions for different error types (authentication failure, vault not found, item not found, field not found, network error, malformed URI)
- **FR-024**: SDK MUST include the context in error messages (which vault, item, or field caused the error)
- **FR-025**: Configuration provider MUST treat all op:// URIs as required and fail fast during configuration building if any secret cannot be retrieved (no optional secrets or default values supported)
- **FR-026**: SDK MUST validate all op:// URI syntax during configuration building (before attempting secret retrieval) and fail immediately if any URI is malformed
- **FR-027**: SDK MUST provide clear error messages for malformed URIs indicating which configuration key contains the invalid URI, what is wrong with the syntax, and the expected format
- **FR-028**: SDK MUST retry transient network failures up to 3 times with exponential backoff (e.g., 1s, 2s, 4s) before throwing a network error exception

**Security:**

- **FR-029**: SDK MUST NOT log, cache, or persist retrieved secret values in plaintext
- **FR-030**: SDK MUST securely handle authentication tokens and ensure they are not exposed in logs or error messages
- **FR-031**: SDK MUST use secure communication channels (HTTPS) for all 1Password API interactions
- **FR-032**: Configuration provider MUST sanitize error messages to prevent leaking partial secret values

### Key Entities *(include if feature involves data)*

- **Vault**: A secure container in 1Password that holds multiple items. Identified by name or unique ID. Users must have permission to access a vault.
- **Item**: An individual entry within a vault that contains one or more secret fields. Identified by name or unique ID within the context of a vault.
- **Secret Field**: A specific field within an item that holds a secret value (e.g., password, API key, connection string). Identified by field name.
- **op:// URI**: A special URI format used in configuration to reference 1Password secrets. Format: `op://<vault>/<item>/<field>` with optional section `op://<vault>/<item>/<section>/<field>`. Components must be URL-encoded if they contain special characters. Conforms to official 1Password secret reference syntax (https://developer.1password.com/docs/cli/secret-reference-syntax/).
- **Configuration Source**: A source of configuration data (appsettings.json, environment variables, command line arguments) that participates in the .NET configuration builder system.
- **Connect Server**: A self-hosted 1Password Connect server that provides API access to 1Password vaults. The SDK authenticates to this server using a Connect server URL and access token.
- **Authentication Configuration**: SDK reads authentication settings from configuration with keys `OnePassword:ConnectServer` (server URL) and `OnePassword:Token` (access token) in appsettings.json, or `OnePassword__ConnectServer` and `OnePassword__Token` as environment variables. Environment variables take precedence.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Developers can integrate 1Password secret retrieval into their .NET application with fewer than 10 lines of code
- **SC-002**: Configuration containing op:// URIs is resolved to actual secrets without requiring custom code for each secret
- **SC-003**: Application startup time increases by less than 500ms when resolving up to 20 secrets from 1Password
- **SC-004**: 95% of developers successfully configure the SDK on their first attempt without consulting detailed documentation
- **SC-005**: Secret retrieval errors provide enough context that developers can identify and fix the issue within 5 minutes
- **SC-006**: Environment variables successfully override 1Password secrets 100% of the time when configured correctly
- **SC-007**: The SDK handles network failures gracefully with clear error messages that indicate the root cause
- **SC-008**: Zero secret values are exposed in application logs, error messages, or debugging output

## Assumptions

- Developers using this SDK have access to a self-hosted 1Password Connect server
- The .NET application has network access to reach the 1Password Connect server
- Developers are familiar with the standard .NET configuration system (Microsoft.Extensions.Configuration)
- 1Password vault and item names are known in advance and referenced correctly in op:// URIs
- Authentication configuration (Connect server URL and token) is provided via appsettings.json or environment variables (OnePassword__ConnectServer, OnePassword__Token)
- Authentication tokens in appsettings.json are protected appropriately (e.g., not committed to source control, encrypted at rest)
- The target .NET version is .NET 8.0 or later (for modern configuration builder support)
- Secret values are text-based and can be represented as strings (not binary data)
- Configuration is built once at application startup (not dynamically rebuilt during runtime)

## Out of Scope

- Automatic rotation or refresh of secrets after application startup
- Graphical user interface for managing 1Password vaults or items
- Support for .NET Framework 4.x (only .NET 6+ will be supported)
- Offline caching of secrets for scenarios without network connectivity
- Integration with configuration systems outside Microsoft.Extensions.Configuration
- Write operations to 1Password (creating, updating, or deleting vaults/items)
- User authentication with 1Password (only service account and Connect server authentication)
- Binary secret storage (files, certificates, images)
- Real-time secret updates during application runtime
- Optional secrets with default values (all op:// URIs are treated as required)
