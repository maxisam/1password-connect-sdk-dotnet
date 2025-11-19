# Data Model: 1Password .NET SDK

**Feature**: 001-onepassword-sdk
**Date**: 2025-11-18
**Source**: Extracted from spec.md and aligned with 1Password Connect API

## Overview

This data model defines the domain entities for the 1Password .NET SDK. The model is derived from:
1. Feature specification entity definitions (spec.md § Key Entities)
2. Official 1Password Connect API schema
3. Reference implementations (JavaScript/Python SDKs)

## Core Entities

### Vault

A secure container in 1Password that holds multiple items.

**Properties**:
```csharp
public class Vault
{
    public string Id { get; set; }              // Unique vault identifier (UUID)
    public string Name { get; set; }            // Display name (user-defined)
    public string Description { get; set; }     // Optional description
    public DateTime CreatedAt { get; set; }     // Creation timestamp
    public DateTime UpdatedAt { get; set; }     // Last modification timestamp
}
```

**Validation Rules**:
- `Id`: Required, non-empty, valid UUID format
- `Name`: Required, non-empty, max 255 characters
- User must have read permission to access a vault (enforced by 1Password API)

**State/Lifecycle**: Immutable from SDK perspective (no create/update/delete operations)

**Relationships**:
- One Vault contains zero or more Items

---

### Item

An individual entry within a vault that contains one or more secret fields.

**Properties**:
```csharp
public class Item
{
    public string Id { get; set; }              // Unique item identifier (UUID)
    public string VaultId { get; set; }         // Parent vault ID
    public string Title { get; set; }           // Display title (user-defined)
    public string Category { get; set; }        // Item category (e.g., "LOGIN", "PASSWORD", "API_CREDENTIAL")
    public IEnumerable<Field> Fields { get; set; }  // Collection of secret fields
    public IEnumerable<Section> Sections { get; set; }  // Optional sections grouping fields
    public DateTime CreatedAt { get; set; }     // Creation timestamp
    public DateTime UpdatedAt { get; set; }     // Last modification timestamp
}
```

**Validation Rules**:
- `Id`: Required, non-empty, valid UUID format
- `VaultId`: Required, must reference an existing vault
- `Title`: Required, non-empty, max 255 characters
- `Category`: Optional, enum-like string (values defined by 1Password)
- `Fields`: Required, must contain at least one field

**State/Lifecycle**: Immutable from SDK perspective (read-only)

**Relationships**:
- One Item belongs to exactly one Vault
- One Item contains one or more Fields
- One Item may contain zero or more Sections

---

### Field

A specific field within an item that holds a secret value or metadata.

**Properties**:
```csharp
public class Field
{
    public string Id { get; set; }              // Unique field identifier (UUID)
    public string Label { get; set; }           // Field name/label (e.g., "password", "username")
    public string Value { get; set; }           // Field value (secret or non-secret)
    public FieldType Type { get; set; }         // Field type (STRING, PASSWORD, URL, etc.)
    public FieldPurpose Purpose { get; set; }   // Field purpose (USERNAME, PASSWORD, etc.)
    public string SectionId { get; set; }       // Optional section this field belongs to
}

public enum FieldType
{
    String,     // Plain text
    Password,   // Concealed text
    Email,      // Email address
    Url,        // URL
    Date,       // Date value
    MonthYear,  // Month and year
    Phone,      // Phone number
}

public enum FieldPurpose
{
    None,       // Generic field
    Username,   // Username field
    Password,   // Password field
    Notes,      // Notes field
}
```

**Validation Rules**:
- `Id`: Required, valid UUID
- `Label`: Required for user-defined fields, may be null for standard fields
- `Value`: May be null or empty (some fields don't have values)
- `Type`: Required, must be valid FieldType
- Field values are text-based, max 1MB size (per spec constraint)

**Security Considerations**:
- Field `Value` contains sensitive data and MUST NOT be logged or persisted in plaintext
- String comparison must be secure (no early exit on mismatch)

**Relationships**:
- One Field belongs to exactly one Item
- One Field may belong to zero or one Section

---

### Section

Logical grouping of fields within an item.

**Properties**:
```csharp
public class Section
{
    public string Id { get; set; }              // Unique section identifier (UUID)
    public string Label { get; set; }           // Section name (user-defined)
}
```

**Validation Rules**:
- `Id`: Required, valid UUID
- `Label`: Required, non-empty

**Relationships**:
- One Section belongs to exactly one Item
- One Section may contain zero or more Fields

---

### SecretReference (op:// URI)

A parsed reference to a 1Password secret using the `op://` URI format.

**Properties**:
```csharp
public class SecretReference
{
    public string Vault { get; set; }           // Vault name or ID
    public string Item { get; set; }            // Item name or ID
    public string Section { get; set; }         // Optional section name
    public string Field { get; set; }           // Field name or label
    public string OriginalUri { get; set; }     // Original op:// URI string

    // Factory method for parsing
    public static bool TryParse(string uri, out SecretReference reference, out string errorMessage);
}
```

**URI Format**:
```
op://<vault>/<item>/<field>
op://<vault>/<item>/<section>/<field>
```

**Validation Rules**:
- Must start with `op://` prefix
- Vault, Item, Field are required components
- Section is optional
- Components must be URL-encoded if they contain special characters (spaces, slashes, etc.)
- Max total URI length: 2048 characters

**Parsing Logic** (`TryParse`):
1. Check `op://` prefix
2. Split remaining path by `/`
3. Validate component count (3 or 4 parts)
4. URL-decode each component
5. Validate no component is empty after decoding
6. Return `SecretReference` if valid, error message otherwise

**Examples**:
- Valid: `op://production/database/password`
- Valid: `op://prod/api-keys/stripe/secret_key`
- Valid: `op://vault/my%20item/field`  (URL-encoded space)
- Invalid: `op://vault/item/`  (empty field)
- Invalid: `http://vault/item/field`  (wrong prefix)

**Relationships**:
- One SecretReference maps to exactly one Field in a specific Item in a specific Vault

---

## Configuration Entities

### OnePasswordClientOptions

Configuration options for the 1Password Connect API client.

**Properties**:
```csharp
public class OnePasswordClientOptions
{
    public string ConnectServer { get; set; }   // Connect server URL (e.g., "https://localhost:8080")
    public string Token { get; set; }           // Service account token for authentication
    public TimeSpan Timeout { get; set; }       // HTTP request timeout (default: 10s)
    public int MaxRetries { get; set; }         // Max retry attempts (default: 3)
}
```

**Validation Rules**:
- `ConnectServer`: Required, valid HTTPS URL
- `Token`: Required, non-empty string
- `Timeout`: Must be > 0, default 10 seconds
- `MaxRetries`: Must be >= 0, default 3

**Configuration Sources** (per spec FR-006, FR-007):
1. **appsettings.json**:
   ```json
   {
     "OnePassword": {
       "ConnectServer": "https://connect.example.com",
       "Token": "your-token-here"
     }
   }
   ```
2. **Environment variables** (take precedence):
   - `OnePassword__ConnectServer`
   - `OnePassword__Token`

---

## Entity Relationships Diagram

```
┌─────────────────────┐
│      Vault          │
│ ─────────────────── │
│ + Id: string        │
│ + Name: string      │
│ + Description       │
└──────────┬──────────┘
           │
           │ 1
           │
           │ *
┌──────────▼──────────┐
│       Item          │
│ ─────────────────── │
│ + Id: string        │
│ + VaultId: string   │
│ + Title: string     │
│ + Category: string  │
└──────┬──────┬───────┘
       │      │
    1  │      │ *
       │      │
    *  │      │ 1
┌──────▼──────▼───────┐
│     Section         │
│ ─────────────────── │
│ + Id: string        │
│ + Label: string     │
└─────────────────────┘
       │
       │ 1
       │
       │ *
┌─────────────────────┐
│      Field          │
│ ─────────────────── │
│ + Id: string        │
│ + Label: string     │
│ + Value: string     │◄────────┐
│ + Type: FieldType   │         │
│ + Purpose           │         │ resolves to
└─────────────────────┘         │
                                │
                     ┌──────────┴──────────┐
                     │  SecretReference    │
                     │ ─────────────────── │
                     │ + Vault: string     │
                     │ + Item: string      │
                     │ + Section?: string  │
                     │ + Field: string     │
                     └─────────────────────┘
```

---

## Validation Summary

| Entity | Required Fields | Max Sizes | Special Rules |
|--------|----------------|-----------|---------------|
| **Vault** | Id, Name | Name: 255 chars | Must have read permission |
| **Item** | Id, VaultId, Title, Fields | Title: 255 chars | At least one field required |
| **Field** | Id, Label, Type | Value: 1MB | Secret values MUST NOT be logged |
| **Section** | Id, Label | Label: 255 chars | Optional grouping |
| **SecretReference** | Vault, Item, Field | URI: 2048 chars | Must start with "op://" |
| **OnePasswordClientOptions** | ConnectServer, Token | - | ConnectServer must be HTTPS |

---

## State Transitions

### Configuration Provider Lifecycle

```
┌──────────────┐
│ Constructed  │
└──────┬───────┘
       │
       │ Load()
       ▼
┌──────────────────────────────┐
│ Scanning for op:// URIs      │
└──────┬───────────────────────┘
       │
       │ Found op:// URIs
       ▼
┌──────────────────────────────┐
│ Parsing URIs                 │
└──────┬───────────────────────┘
       │
       │ All URIs valid
       ▼
┌──────────────────────────────┐
│ Batch Retrieving Secrets     │  ◄─── Retry on transient failure (3x)
└──────┬───────────────────────┘
       │
       │ All secrets retrieved
       ▼
┌──────────────────────────────┐
│ Secrets Cached (Immutable)   │
└──────────────────────────────┘
       │
       │ Application reads config
       ▼
┌──────────────────────────────┐
│ Returning Cached Values      │
└──────────────────────────────┘
```

**Failure States**:
- **Malformed URI**: Fail immediately before API call (FR-028, FR-031)
- **Network Error**: Retry 3x with exponential backoff, then fail (FR-033)
- **Secret Not Found**: Fail immediately with context (FR-026, FR-029)
- **Timeout**: Fail after 10s with timeout error (FR-024)

---

## Implementation Notes

### Immutability
All domain entities (Vault, Item, Field, Section) should be implemented as immutable classes to prevent accidental modification after retrieval. Use `init` accessors in C#:

```csharp
public class Vault
{
    public string Id { get; init; }
    public string Name { get; init; }
    // ...
}
```

### Serialization
Use `System.Text.Json` attributes for JSON serialization/deserialization:

```csharp
[JsonPropertyName("id")]
public string Id { get; init; }
```

### Security
Field values containing secrets MUST be marked as sensitive and excluded from logging:

```csharp
// Use custom ToString() override to prevent accidental logging
public override string ToString() => $"Field(Id={Id}, Label={Label}, Type={Type})";
```

---

## Alignment with Specification

| Spec Entity | Data Model Class | Status |
|-------------|------------------|--------|
| Vault (spec.md:143) | `Vault` | ✅ Implemented |
| Item (spec.md:144) | `Item` | ✅ Implemented |
| Secret Field (spec.md:145) | `Field` | ✅ Implemented |
| op:// URI (spec.md:146) | `SecretReference` | ✅ Implemented |
| Configuration Source (spec.md:147) | N/A (handled by Microsoft.Extensions.Configuration) | - |
| Connect Server (spec.md:148) | `OnePasswordClientOptions.ConnectServer` | ✅ Implemented |
| Authentication Configuration (spec.md:149) | `OnePasswordClientOptions` | ✅ Implemented |

All entities from specification are represented in the data model.
