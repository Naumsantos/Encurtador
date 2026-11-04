# Encurtador Project Constitution

## Core Principles

### I. Application-Service Boundary
HTTP endpoints MUST delegate use cases to application services. Endpoints MUST NOT
access EF Core, a DbContext, or persistence directly. This separation keeps transport
and data-access concerns out of endpoint code.

### II. Business Rules Belong Outside HTTP
Business rules MUST NOT be implemented in endpoints or HTTP handlers. They MUST live
in application or domain logic invoked by the application services. This keeps
business behavior independent of HTTP details.

### III. EF Core Persistence
Persistence MUST use Entity Framework Core with a replaceable provider. Provider-specific
SQL MUST NOT be scattered through application code; persistence concerns belong behind
the persistence boundary. This preserves the ability to change providers without
rewriting application behavior.

### IV. Required Automated Tests
Every business rule MUST have unit-test coverage using xUnit. Every endpoint MUST have
at least one integration test for its successful path. These requirements make business
behavior and the HTTP contract verifiable.

### V. Consistent Validation Responses
Every validation failure MUST return HTTP 400 with a ProblemDetails response. Endpoints
MUST apply this contract consistently so clients can handle invalid requests predictably.

### VI. Documented API Contract
Every API endpoint MUST be documented with Swagger/OpenAPI. The frontend MUST consume
only endpoints documented by this contract. This keeps frontend integration aligned
with the supported API surface.

### VII. Frontend Separation
Business rules MUST NOT live in the frontend. A `.razor` component MUST contain only
presentation logic; API calls and shared state MUST be handled by injected services.
This keeps business behavior centralized and components focused on rendering and
interaction.

### VIII. Frontend User Experience
Every screen MUST handle loading, error, and empty states. All text displayed to users,
including errors, MUST be in Portuguese so that each screen communicates its state
consistently to the intended audience.

### IX. Frontend Automated Tests
Components with critical interactions MUST have tests using bUnit and xUnit. Each main
user flow MUST have at least one end-to-end test using Playwright. These tests verify
both component behavior and complete user journeys.

## Additional Constraints

- The application MUST target .NET 10, use C#, and expose HTTP functionality through
  Minimal APIs.
- The frontend MUST use Blazor Server on .NET 10 with MudBlazor as its only UI library.
  Its project MUST be `src/Encurtador.Web` in the same solution.
- Every new dependency MUST have a written justification in the implementation plan
  before it is introduced.
- Any new UI package MUST have a written justification in the implementation plan.
- Class, file, and component names MUST be in English. All text displayed to users
  MUST be in Portuguese.

## Development Workflow

- Plans, implementation, and reviews MUST comply with this constitution. If a plan
  conflicts with a principle, the plan MUST be changed; the principle MUST NOT be
  weakened to accommodate the plan.
- API and frontend commits MUST use the existing format
  `tipo(escopo): descrição [REQ-ID]`, including the applicable requirement identifier.
- Changes to business rules and endpoints MUST satisfy the corresponding test
  requirements before they are considered complete.
- Frontend changes MUST satisfy the component and end-to-end test requirements before
  they are considered complete.

## Governance

This constitution governs project plans, implementation, and review. Amendments MUST
state their rationale, be approved by project maintainers, update the version and
amendment date, and preserve the requirements above unless the amendment explicitly
changes them. Any conflicting plan or project guidance MUST be revised to comply.

The constitution version follows semantic versioning: MAJOR for backward-incompatible
principle changes, MINOR for new principles or materially expanded requirements, and
PATCH for clarifications and non-semantic wording changes. Project maintainers MUST
review compliance during plan review and implementation review, including the required
test coverage and dependency justifications.

**Version**: 1.1.0 | **Ratified**: 2026-11-04 | **Last Amended**: 2026-11-04
