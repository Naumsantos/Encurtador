# Data Model: Encurtador de links

## ShortLink

Represents a stored mapping from a short code to its destination.

| Field | Type | Constraints and meaning |
|---|---|---|
| `Id` | `Guid` | Application-generated primary key; avoids provider-specific identity/sequence behavior. |
| `Code` | string (max 20) | Required, case-sensitive short code; explicitly unique database index. Automatically generated values have exactly 7 characters from `23456789ABCDEFGHJKMNPQRSTVWXYZ`; chosen values follow the spec's 4–20 ASCII letters/digits/hyphen/underscore rule. |
| `DestinationUrl` | string (max 2048) | Required absolute HTTP or HTTPS URL. |
| `CreatedAtUtc` | UTC `DateTime` | Required creation instant, normalized to UTC before persistence. |
| `ExpiresAtUtc` | nullable UTC `DateTime` | Optional expiration instant, normalized to UTC; must be strictly later than creation time. A link is expired when `ExpiresAtUtc <= now`. |
| `AccessCount` | `long` | Required, starts at zero, and never decreases. Increased only by an atomic database-side update for a valid access. |

### Indexes

- Primary key on `Id`.
- Unique index on `Code`. The database constraint, not the service's pre-check, is the final uniqueness guarantee under concurrent creation.

### Relationships and lifecycle

There are no relationships in the first version. A link is created active when it has no expiration or its expiration is in the future. If expiration is set, it transitions logically to expired at that instant; no background job or state mutation is needed. Expired links remain stored and their statistics remain queryable, but redirect requests do not increment the count.

### Management credential boundary

No management credential is included in this feature's entity or API: the specification defines no management action, and statistics are public by code. If a later feature introduces a management key, persist only its cryptographic hash, never the raw value.

## Validation and persistence invariants

- Code comparison and uniqueness preserve case sensitivity.
- URL, code, and expiration checks occur before persisting a new link.
- Any requested-code collision is rejected; an automatically generated collision causes a bounded generation retry.
- The database unique index handles collisions that occur after the service availability check.
- Hit increments are expressed as `AccessCount = AccessCount + 1` in one update statement, conditioned on the code and current non-expired status. The application never reads and rewrites the count for a redirect.
- UTC `DateTime` storage avoids relying on SQLite's limited `DateTimeOffset` ordering behavior. HTTP clients may send ISO 8601 values with offsets; the service converts them to UTC instants.
