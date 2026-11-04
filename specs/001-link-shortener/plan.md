# Implementation Plan: Encurtador de links

**Branch**: `001-link-shortener` | **Date**: 2026-11-03 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-link-shortener/spec.md` and architecture decisions provided for planning.

## Summary

Deliver a .NET 10 Minimal API for creating short links, redirecting active links, and reading public access totals. Keep HTTP handlers thin and place business rules in an application-layer `LinkService`. Use EF Core with SQLite for the first version, behind a persistence boundary that can later use PostgreSQL. Persist link codes under a unique database index and increment access totals with a single atomic database update.

## Technical Context

**Language/Version**: C# on .NET 10
**Primary Dependencies**: ASP.NET Core Minimal APIs; EF Core 10 SQLite provider; xUnit; `Microsoft.AspNetCore.Mvc.Testing` for endpoint integration tests. Each dependency is justified by the required API, relational persistence, or tests. Add EF Core design-time tooling only to manage migrations. Do not add a PostgreSQL provider until that provider is introduced.
**Storage**: SQLite initially, using EF Core and provider-neutral application contracts; PostgreSQL is a future provider option. Keep provider-specific migrations separate.
**Testing**: xUnit unit tests for `LinkService`; xUnit integration tests through `WebApplicationFactory` against SQLite, not EF Core InMemory.
**Target Platform**: ASP.NET Core service on platforms supported by .NET 10.
**Project Type**: Minimal API web service.
**Performance Goals**: No minimum simultaneous-access capacity is specified. A valid successful redirect must increment the stored total by exactly one.
**Constraints**: HTTP/HTTPS absolute destination URLs up to 2048 characters; custom codes are 4–20 ASCII letters/digits/hyphen/underscore and case-sensitive; generated codes are 7 alphanumeric characters. Expiration is an instant and is exclusive (expired when `ExpiresAtUtc <= now`). No account, custom-domain, geographic-reporting, destination-editing, or anonymous-creation-rate-limit features.
**Scale/Scope**: One resource (links) with create, redirect, and public statistics operations. SQLite serializes writers; contention must be surfaced or handled with an explicit bounded provider-specific policy, never hidden as a successful count.

## Key Design Decisions

| Decision | Rationale |
|---|---|
| Check code availability in `LinkService` and enforce a unique database index on `Code`. | The service check provides an early, clear response, but cannot prevent a concurrent check-then-insert race. The database index is the correctness guarantee. Automatic collisions retry; a collision on a requested code is a validation failure. |
| Increment `AccessCount` with one database-side update (`AccessCount = AccessCount + 1`). | A read-modify-write in application memory can lose increments when requests overlap. A single atomic `UPDATE` avoids that race and does not load/rewrite the count in application code. |
| Store any future management credential only as a one-way hash, never plaintext. | A database disclosure must not reveal a reusable credential. If a management key is later introduced, generate a high-entropy random token and persist only its cryptographic hash; do not log the raw token. The current specification defines no management operation or key lifecycle, so this plan adds no management-key field or endpoint. |
| Generate codes from `23456789ABCDEFGHJKMNPQRSTVWXYZ`. | This uppercase, 30-symbol alphanumeric alphabet excludes `0`, `1`, `I`, `L`, `O`, and `U`, avoiding common visual confusions. User-chosen codes continue to follow the broader character set explicitly allowed by the specification. |
| Keep persistence behind an application-owned store interface; select SQLite in API composition. | This keeps EF/provider-specific behavior outside `LinkService` and HTTP endpoints. Infrastructure translates recognized unique-index violations into a provider-neutral conflict result; provider-specific migrations remain isolated when adding PostgreSQL. |
| Retry transient SQLite busy/locked errors up to three times with short bounded backoff; return HTTP 503 ProblemDetails in Portuguese if all attempts fail. | SQLite serializes writers. A bounded retry can absorb brief contention while preserving exact counting; exhausted retries must not be reported as a successful redirect or count. |
| Normalize timestamps to UTC and persist as UTC `DateTime`. | SQLite has partial query support for `DateTimeOffset`; normalizing instants to UTC avoids provider-dependent comparison behavior. HTTP input/output remains ISO 8601 date-time with offset support on input. |
| Map malformed or missing JSON bodies to HTTP 400 ProblemDetails in Portuguese via `ApiExceptionHandler`. | Constitution V requires every validation failure to be a 400 ProblemDetails; without it, the framework returns English text with exception details. |
| Configure one canonical `Links:PublicBaseUrl` for response links. | Creation must return a shareable absolute short URL without trusting a caller-controlled `Host` header. This is deployment configuration, not support for custom domains. |

## Constitution Check

### Pre-design gate

| Principle / constraint | Status | Plan response |
|---|---|---|
| HTTP endpoints delegate use cases to application services. | PASS | `LinksEndpoints` maps requests to `LinkService`; no endpoint accesses EF Core. |
| Business rules stay outside HTTP handlers. | PASS | Validation, code generation, uniqueness handling, expiry checks, and hit accounting belong to `LinkService` and its collaborators. |
| EF Core provider remains replaceable. | PASS | Application-owned persistence interface; SQLite-specific configuration, migrations, and exception classification stay in Infrastructure/composition. |
| xUnit coverage for business rules and successful endpoint paths. | PASS | Unit tests cover `LinkService`; integration tests cover each endpoint's successful path using SQLite. |
| Validation errors are HTTP 400 ProblemDetails; user-facing errors are Portuguese. | PASS | Contract uses ProblemDetails for creation validation and returns Portuguese `title`/`detail`. |
| .NET 10, C#, Minimal APIs; dependencies justified. | PASS | Stack is explicit above; dependency purposes are documented in Technical Context. |

No unresolved requirements or constitution violations block design.

### Post-design gate

PASS. The proposed project boundaries preserve the application-service and provider boundaries. Contracts use 400 ProblemDetails for validation, test strategy uses xUnit and real relational SQLite, and the four supplied architectural decisions are recorded with justification. SQLite busy handling and provider-specific unique-constraint detection are explicit infrastructure concerns; `LinkService` receives a provider-neutral conflict result.

## Project Structure

### Documentation (this feature)

```text
specs/001-link-shortener/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
└── contracts/
    └── openapi.yaml
```

### Source Code (repository root)

```text
src/
├── Encurtador.Api/
│   ├── Program.cs
│   ├── Contracts/
│   └── Endpoints/
│       └── LinksEndpoints.cs
├── Encurtador.Application/
│   ├── LinkService.cs
│   ├── Links/
│   └── Persistence/
│       └── ILinkStore.cs
└── Encurtador.Infrastructure/
    └── Persistence/
        ├── LinksDbContext.cs
        ├── EfLinkStore.cs
        └── Migrations/
            └── Sqlite/
tests/
├── Encurtador.UnitTests/
└── Encurtador.IntegrationTests/
```

**Structure Decision**: Use three production projects to keep transport, application rules, and EF Core/provider configuration separate. `LinksEndpoints.cs` is the single endpoint-registration file for the link resource; it maps creation, redirection, and statistics routes. Unit and integration tests are separated by their service-vs-HTTP/database boundaries. Keep the application entity and persistence port in the Application project to avoid an extra domain project for one resource.

## Complexity Tracking

No constitution violations or additional architectural layers require justification.
