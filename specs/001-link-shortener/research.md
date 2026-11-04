# Research: Encurtador de links

**Feature**: `001-link-shortener`  
**Date**: 2026-11-03

## Minimal APIs organized by resource

**Decision**: Register link routes from one `LinksEndpoints` extension method/file, grouping the resource routes and keeping route handlers limited to HTTP binding and response mapping.

**Rationale**: ASP.NET Core Minimal APIs support route groups and route-registration extensions. The requested one-file-per-resource organization is a simple, framework-native way to avoid putting all routes and business logic in `Program.cs`.

**Alternatives considered**: A single large `Program.cs` was rejected because it mixes resource routes and transport mapping. Carter or FastEndpoints were not selected because the first version has one resource and does not justify another runtime dependency.

**Source**: [Minimal API route handlers and groups](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/route-handlers?view=aspnetcore-10.0).

## Application service owns the business rules

**Decision**: `LinkService` owns creation validation, generated-code selection and retry, expiry checks, and recording of valid accesses. The endpoint layer calls the service and maps its result to HTTP.

**Rationale**: This supports direct unit testing of business behavior and keeps HTTP types out of application rules. Persistence is reached through an application-owned interface implemented by Infrastructure.

**Alternatives considered**: Fat endpoint handlers were rejected because they couple rules to HTTP and make focused unit tests difficult. A mediator/CQRS framework was not selected for this single-resource use case because it adds indirection and a dependency without a demonstrated need.

**Source**: [ASP.NET Core integration testing](https://learn.microsoft.com/aspnet/core/test/integration-tests?view=aspnetcore-10.0) describes the application under test and full-pipeline integration boundary.

## EF Core with SQLite first and a PostgreSQL migration path

**Decision**: Keep application logic on the common relational EF Core surface, configure SQLite in the API composition root, and isolate provider-specific concerns in Infrastructure. Keep migrations provider-specific; a later PostgreSQL provider will need its own provider-specific migration set.

**Rationale**: Replacing the provider is not just changing a connection string: EF Core providers translate operations differently, and SQLite has modeling/query limitations and schema migration restrictions. Avoid SQLite-specific SQL in the application layer, use UTC `DateTime` values for persisted instants, and verify provider behavior with integration tests.

**Alternatives considered**: EF Core InMemory was rejected for integration tests because it is not a relational database and does not prove unique-index or real provider behavior. Raw SQL for core rules was rejected in favor of EF Core's provider-translated relational operations.

**Sources**:
- [EF Core SQLite provider limitations](https://learn.microsoft.com/ef/core/providers/sqlite/limitations)
- [EF Core provider and database behavior](https://learn.microsoft.com/ef/core/)

## Unique short-code constraint under concurrency

**Decision**: Perform an availability check in the service for a fast, clear common path and enforce an explicitly named unique index on `Code` in the database. Infrastructure translates a recognized unique-index violation to a provider-neutral conflict result; `LinkService` retries with a new generated code or returns a validation conflict for a requested code. Bound automatic retries and surface unrelated persistence failures.

**Rationale**: A service-level check cannot serialize concurrent creates; the unique index is the final integrity guarantee. Provider error types differ, so detection must not depend on message text or SQLite-only exceptions in `LinkService`.

**Alternatives considered**: A pre-check without a database index was rejected because two requests can both pass it. An index without an availability check would remain correct, but can turn a normal requested-code conflict into an avoidable persistence exception.

**Sources**:
- [EF Core concurrency and save behavior](https://learn.microsoft.com/ef/core/saving/concurrency)
- [SQLite result codes](https://www.sqlite.org/rescode.html#constraint)
- [PostgreSQL SQLSTATE codes](https://www.postgresql.org/docs/current/errcodes-appendix.html)

## Atomic access counting

**Decision**: Increment the persisted access count with one EF Core `ExecuteUpdateAsync` expression that adds one to the current database value, filtered by code and non-expired status. Do not load, increment, and save the count in application memory. Read the destination separately; only redirect after the conditional update reports one affected row.

**Rationale**: The database-side expression prevents lost increments from concurrent read-modify-write operations. Including the expiry predicate in the update prevents an access after expiry from being counted; the destination may be read without loading the count.

**Alternatives considered**: Tracked entity read/increment/save was rejected because concurrent requests can overwrite each other's count. Raw SQL was not selected because EF Core can translate the update while keeping it in the provider-neutral relational abstraction. A background batching queue was rejected because it would change the immediate exact-count behavior.

**Operational note**: SQLite permits only one writer at a time. Configure a reasonable busy timeout and treat a busy/locked result as an explicit SQLite infrastructure concern; if retries are added, keep them bounded and provider-specific. PostgreSQL has different concurrency and retry behavior. The feature defines no minimum concurrency capacity.

**Sources**:
- [EF Core bulk update](https://learn.microsoft.com/ef/core/saving/execute-insert-update-delete)
- [SQLite write-ahead logging concurrency](https://www.sqlite.org/wal.html)
- [SQLite busy timeout](https://www.sqlite.org/c3ref/busy_timeout.html)

## Management credential storage

**Decision**: Any management credential, if a later specified flow introduces one, is persisted only as a one-way cryptographic hash. For a server-generated, high-entropy random token, a fast cryptographic hash is appropriate; never log or persist the raw token. The current feature specification has no management operation or key lifecycle, while public statistics explicitly require no authentication, so no key field or management endpoint is designed here.

**Rationale**: A one-way hash permits verification without retaining a reusable plaintext credential. Human-selected low-entropy secrets have a different threat model and require an appropriate password-hashing strategy; that is not assumed for a future generated token.

**Alternatives considered**: Plaintext storage and reversible encryption were rejected because the application only needs to verify a presented credential, not recover it. HMAC/peppering could be considered if a future threat model and secret-rotation design require it.

**Source**: [OWASP REST Security Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/REST_Security_Cheat_Sheet.html).

## Generated code alphabet

**Decision**: Use `23456789ABCDEFGHJKMNPQRSTVWXYZ` for generated codes: uppercase letters and digits, excluding visually confusable `0`, `1`, `I`, `L`, `O`, and `U`. Generated codes remain exactly seven characters; user-selected codes retain the broader syntax in the specification.

**Rationale**: Restricting the output alphabet makes generated links easier to read and transcribe. Crockford's Base32 documents the excluded letter rationale; the selected 30-symbol alphabet additionally omits digits `0` and `1` so the generated codes themselves contain no ambiguous glyphs.

**Alternatives considered**: Base62 was rejected because it includes common ambiguous pairs. Canonical Crockford Base32 retains digits `0` and `1` (with decoding aliases), so the stricter 30-symbol variant is used for generated output.

**Source**: [Crockford Base32](https://www.crockford.com/base32.html).

## xUnit test boundaries

**Decision**: Unit-test `LinkService` with a test double for the persistence port. Integration-test every route's successful path against the real SQLite provider using `WebApplicationFactory`; include database constraint and atomic-update behavior in integration coverage.

**Rationale**: Unit tests isolate business rules and make collisions/retries deterministic. Real SQLite tests verify relational constraints and generated SQL that an in-memory fake cannot validate.

**Alternatives considered**: EF Core InMemory was rejected because it does not exercise SQLite's unique index or update semantics. PostgreSQL integration tests are deferred until PostgreSQL support is actually introduced.

**Source**: [ASP.NET Core integration testing](https://learn.microsoft.com/aspnet/core/test/integration-tests?view=aspnetcore-10.0).
