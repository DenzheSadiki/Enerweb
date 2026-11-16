# Architecture Decision Document
## Enerweb Application Architecture

**Document type:** Architecture Decision Document (ADD)  
**Status:** Accepted  
**Application:** Enerweb  
**Architecture style:** Layered architecture with repository and service abstractions  
**Database:** SQL Server  
**Framework:** .NET 8  
**Last updated:** 16 November 2026

---

# 1. Purpose

This document establishes the architectural decisions, principles and implementation conventions governing the Enerweb application.

The objective is to provide a maintainable, extensible and testable application architecture that separates business logic, persistence, API concerns and infrastructure responsibilities.

The architecture should support the current functional requirements without unnecessarily constraining future development or deployment strategies.

The application will initially operate as a single deployable application. Decisions regarding future deployment models, including distributed services or microservices, remain outside the scope of this document.

# 2. Architectural Principles

The following principles guide architectural decisions:

1. **Separation of concerns:** Each layer must have a clearly defined responsibility.
2. **Dependency direction:** Higher-level application concerns should depend on abstractions rather than concrete infrastructure implementations.
3. **Reusability:** Common functionality should be implemented once and reused where appropriate.
4. **Extensibility:** New entities and business capabilities should be introduced without requiring significant changes to existing functionality.
5. **Testability:** Business logic and persistence operations must be independently testable.
6. **Data integrity:** Application validation and database constraints must work together to protect data integrity.
7. **Security:** Data access and reporting functionality must enforce explicit validation and authorization boundaries.
8. **Pragmatic abstraction:** Abstractions should provide meaningful architectural boundaries rather than duplicate existing framework functionality.

# 3. Solution Architecture

## 3.1 Project responsibilities

The solution is divided into the following projects:

| Project | Responsibility |
|---|---|
| Enerweb.Api | HTTP endpoints, request handling and dependency composition |
| Enerweb.Services | Business logic, validation and application workflows |
| Enerweb.Interfaces | Service and repository contracts, Unit of Work abstraction |
| Enerweb.Domain.Models | Domain entities and request/response DTOs |
| Enerweb.Data | EF Core, database context, repositories and persistence |
| Enerweb.Middleware | Centralized exception handling and request logging |
| Enerweb.Data.Tests | Persistence and integration testing |
| Enerweb.Api.Tests | HTTP integration testing |

## 3.2 Project dependency direction

The intended dependency structure is:

```text
Enerweb.Api
    |
    +----> Enerweb.Services
    |           |
    |           +----> Enerweb.Interfaces
    |           |           |
    |           |           +----> Enerweb.Domain.Models
    |           |
    |           +----> Enerweb.Domain.Models
    |
    +----> Enerweb.Data
    |           |
    |           +----> Enerweb.Interfaces
    |           |
    |           +----> Enerweb.Domain.Models
    |
    +----> Enerweb.Middleware
```

The API references the Data project exclusively to configure persistence through the composition root.

Controllers must not directly access repositories or the database context. They interact with service contracts.

Services may reference EF Core query extensions to compose asynchronous LINQ queries, but must not reference the Data project or its database context.

The architecture must preserve these dependency boundaries.

# 4. Repository Pattern

## 4.1 Decision

The application will use a generic repository implementation for common CRUD operations, supplemented by dedicated repository contracts where entity-specific data access is required.

The generic repository provides reusable functionality without requiring each entity to reimplement common operations.

Dedicated repository contracts provide a consistent abstraction for entity-specific persistence requirements.

## 4.2 Repository responsibilities

Repositories are responsible for:

- Retrieving entities.
- Adding entities.
- Updating entities.
- Removing entities.
- Providing composable queries.
- Managing EF Core entity tracking behaviour.

Repositories must not contain business workflows or independently commit database changes.

## 4.3 Query behaviour

`IBaseRepository<TEntity>.Query()` returns a deferred, no-tracking query.

`QueryTracking()` provides a tracked query for operations requiring entity updates.

`GetByIdAsync(Guid)` retrieves a tracked entity and returns null when the requested record does not exist.

Repository methods must reject null entity arguments.

Repositories must not invoke `SaveChangesAsync()`.

## 4.4 Repository implementation conventions

Each entity-specific repository inherits from the shared generic implementation.

For example:

```csharp
public interface ITariffRepository
    : IBaseRepository<Tariff>
{
    // Tariff-specific data operations
}
```

```csharp
public class TariffRepository
    : BaseRepository<Tariff>,
      ITariffRepository
{
    public TariffRepository(
        ApplicationDbContext context)
        : base(context)
    {
    }
}
```

Dedicated repository contracts and their generic aliases must resolve to the same scoped repository instance.

This ensures consistent entity tracking and persistence behaviour.

# 5. Service Layer

## 5.1 Decision

Business logic and application workflows will reside in the Services project.

Services are responsible for:

- Business validation.
- Coordinating repository operations.
- Enforcing business rules.
- Composing data queries.
- Managing application workflows.
- Committing persistence operations through Unit of Work.

Controllers must not implement business logic.

## 5.2 Service conventions

Each domain model should have a corresponding service contract and implementation.

Generic service functionality should be reused where appropriate, with entity-specific services extending the shared functionality.

Services may coordinate multiple repositories within a single business operation.

A service must not assume that independently invoked CRUD service methods participate in a shared transaction.

When multiple repository operations form one business workflow, the service should stage the changes and commit them together.

# 6. Unit of Work and Transaction Management

## 6.1 Decision

The application will use the Unit of Work pattern to coordinate persistence across multiple repositories.

The Unit of Work is responsible for committing staged changes through the shared EF Core database context.

## 6.2 Implementation principles

All repositories and Unit of Work instances within a dependency injection scope must share the same `ApplicationDbContext`.

The database context, repositories and Unit of Work are registered with scoped lifetimes.

A typical workflow:

```csharp
await _siteRepository.AddAsync(site);

foreach (var meter in meters)
{
    await _meterRepository.AddAsync(meter);
}

await _unitOfWork.SaveChangesAsync();
```

All staged changes are committed through a single `SaveChangesAsync()` operation.

EF Core provides transaction protection for a single save operation when supported by the relational provider.

If any operation within that save fails, the transaction is rolled back.

## 6.3 Transaction boundaries

Initial service CRUD operations commit their individual operations.

Larger business workflows should coordinate repository operations directly and commit once.

Services must not chain independent CRUD service methods when atomic transaction behaviour is required.

Explicit database transactions may be introduced when a workflow genuinely requires multiple save operations to share one transaction.

A failed operation must not be treated as a recovery boundary for subsequent unrelated writes. The affected dependency injection scope should be disposed.

Parallel database operations must not execute against the same scoped context.

Deferred queries must be materialized before the scope is disposed.

`IQueryable` must never be returned directly to API clients.

# 7. Reporting Architecture

## 7.1 Context

The application requires reporting capabilities that extend beyond conventional entity CRUD operations.

Reporting functionality must support:

- Selecting reportable entities.
- Selecting available fields.
- Filtering and sorting.
- Aggregations and grouping.
- Pagination.
- Future dynamic report generation.
- Potential integration with natural-language report requests.

Reporting is considered a distinct application capability rather than an extension of generic CRUD functionality.

## 7.2 Current decision

The application will maintain a dedicated `ReportRequest` entity persisted in the `Reports` table.

Report requests are managed through:

- `IReportRepository`
- `ReportRepository`
- `IReportService`
- `ReportService`

The `Reports` table is introduced through the `AddReports` migration.

The report definition is stored as text in `QueryDefinition`.

The stored definition represents a report specification and must not be executed directly as SQL.

## 7.3 Report request validation

The following requirements apply:

| Property | Requirement |
|---|---|
| Name | Required, maximum 200 characters |
| Entity | Required, maximum 100 characters |
| QueryDefinition | Required |
| RequestedBy | Required, nonempty identifier |
| CreatedAt | Assigned in UTC |
| LastExecutedAt | Cleared on creation |

Updates must preserve ownership, creation timestamps and execution history.

## 7.4 Future reporting capabilities

The reporting execution engine is not currently implemented.

Future implementation must introduce appropriate validation and execution boundaries.

The following principles are established for future development:

1. Reportable entities and fields must be explicitly allowlisted.
2. Query operators must be restricted to supported operations.
3. User-supplied values must be parameterized.
4. Access to underlying data must be authorized independently of query validation.
5. Pagination and execution limits must be enforced.
6. Arbitrary client SQL and unvalidated expression strings must never be executed.
7. Reporting queries should remain separate from conventional CRUD repository responsibilities.

The architecture should allow reporting to support both structured report requests and future natural-language interpretation without exposing unrestricted database access.

The separation of saved report definitions and individual report executions, including execution history and asynchronous processing, remains a future design decision.

# 8. Data Integrity and Business Rules

Application-level validation and database constraints will work together to maintain data integrity.

Application validation provides meaningful feedback, while database constraints remain authoritative for concurrent writes.

## 8.1 Sites

- A site must have a name.
- A site must have a unique code.
- Site codes are limited to 50 characters.
- Sites containing meters cannot be deleted.

## 8.2 Meters

- A meter must reference an existing site.
- Required identifying fields must be supplied.
- Meter numbers must be unique.
- Meter numbers are limited to 100 characters.
- Current readings must not be less than previous readings.
- Consumption is calculated from the difference between current and previous readings.
- Meters containing readings cannot be deleted.

## 8.3 Readings

- A reading must reference an existing meter.
- Reading values cannot be negative.
- Reading timestamps cannot be in the future.
- A meter may have only one reading at each UTC timestamp.
- The same timestamp across different meters is valid.

A unique composite database index enforces the meter/timestamp constraint.

Service validation provides an earlier conflict check, excluding the current record during updates.

Historical reading edits do not automatically recalculate meter summary fields.

## 8.4 Entity updates

Updates must copy explicitly permitted scalar fields onto the existing tracked entity.

Entity identifiers and creation timestamps must be preserved.

Child entities should be managed independently rather than submitted through nested creation graphs.

## 8.5 Database conflicts

Validation queries cannot eliminate concurrency races.

Database unique constraints remain authoritative.

Callers must handle `DbUpdateException` at the API boundary.

Database implementation details must not be exposed in HTTP responses.

# 9. API Architecture and HTTP Conventions

## 9.1 Controller responsibilities

Controllers expose the application functionality through HTTP endpoints.

A shared abstract `CrudController<TEntity>` provides common CRUD behaviour.

Concrete controllers select the appropriate service and entity identifier property.

Controllers must inject service contracts only.

## 9.2 CRUD conventions

All five resources expose consistent CRUD operations.

| Operation | HTTP method | Route |
|---|---|---|
| Retrieve all | GET | `/api/{Resource}` |
| Retrieve one | GET | `/api/{Resource}/{id}` |
| Create | POST | `/api/{Resource}` |
| Update | PUT | `/api/{Resource}/{id}` |
| Delete | DELETE | `/api/{Resource}/{id}` |

Resource routes:

- `/api/Sites`
- `/api/Meters`
- `/api/Readings`
- `/api/Addresses`
- `/api/ContactPeople`
- `/api/Reports`

Identifiers are GUIDs.

Create operations return HTTP 201 with a Location header.

Successful update and delete operations return HTTP 204.

Missing records return 404, invalid requests return 400 and business conflicts return 409.

Create and update requests contain scalar fields.

Meters reference sites through `siteId`, and readings reference meters through `meterId`.

Navigation objects are not required in request bodies.

The initial API endpoints inherit the application's existing access policy. Authentication is not currently configured.

# 10. Error Handling and Request Logging

## 10.1 Decision

Centralized exception handling and request logging are implemented through `Enerweb.Middleware`.

`UseErrorLogging()` replaces the previous MVC exception filter.

The middleware provides consistent error responses and request logging.

## 10.2 Error response conventions

Caught exceptions are mapped to ProblemDetails responses.

| Exception category | HTTP status |
|---|---|
| Validation / argument errors | 400 |
| Missing records | 404 |
| Business or database conflicts | 409 |
| Unexpected exceptions | 500 |

Responses include a trace identifier.

Unexpected exception details and database implementation information must not be returned to clients.

Responses that have already started must not be rewritten.

Client-request cancellation is rethrown without generating an exception record.

## 10.3 Logging persistence

`ILogService` and `LogService` coordinate the persistence of:

- `Error`
- `DatabaseLog`

Both are staged through their respective repositories and committed together through Unit of Work.

The `AddErrorLogging` migration introduces the Errors and DatabaseLogs tables, including their foreign key and timestamp/trace indexes.

Logging is performed in a fresh dependency injection scope to isolate it from failed or pending request database operations.

Database logging has a five-second cancellation timeout.

If database logging fails, the application logger provides a fallback without replacing the original HTTP response or recursively retrying the failed operation.

## 10.4 Request logging conventions

Every HTTP request produces one DatabaseLog, including successful responses, redirects, unmatched routes and exceptions.

Client cancellations are recorded with status 499.

Only the following request information is captured:

- Request path.
- HTTP method.
- Response status.
- Duration in milliseconds.
- Trace identifier.

Headers, query strings and request bodies are excluded.

ErrorId is nullable for requests without associated exceptions.

Requests without exceptions do not create Error records.

Logging timestamps are assigned in UTC.

There are no public CRUD endpoints for logging entities.

# 11. Date and Time Conventions

All application `DateTime` timestamps are treated as UTC.

The API accepts ISO 8601 timestamps.

Explicit timezone offsets are normalized to UTC. Timestamps without an offset are interpreted as UTC.

Responses use the ISO 8601 round-trip format.

Example:

```text
2026-11-16T10:30:00.0000000Z
```

EF Core normalizes local values on write and restores `DateTimeKind.Utc` on read, including nullable timestamps.

Existing timezone-free SQL `datetime2` values are interpreted as UTC.

Database column types remain unchanged.

Date-only values are unaffected.

# 12. Configuration and Deployment

## 12.1 Database configuration

Development configuration is loaded from:

```text
Enerweb.Data.Databaase/database.local.json
```

Individual database settings can be overridden by user secrets, environment variables and command-line arguments.

Deployment environments must supply database configuration externally.

Supported configuration includes:

```text
ConnectionStrings__DefaultConnection

Database__Host
Database__Port
Database__Name
Database__Username
Database__Password
Database__Encrypt
Database__TrustServerCertificate
```

Local credentials must not be copied into build output.

## 12.2 Database migrations

Database migrations are an explicit deployment responsibility.

The application must not automatically apply migrations during API startup.

Migrations must be reviewed and applied as a controlled deployment step.

## 12.3 API documentation

Swashbuckle provides the Development OpenAPI document at:

```text
/openapi/v1.json
```

The existing weather controller is retained.

# 13. Testing Strategy

Testing is divided into persistence-level and API-level testing.

## 13.1 Data tests

`Enerweb.Data.Tests` validates repository and service behaviour.

Tests use isolated SQLite databases with foreign keys enabled.

Coverage includes:

- CRUD operations.
- Entity validation.
- Business rules.
- Repository registration and shared scoped instances.
- Multiple repository operations.
- Transaction rollback.

A SQL Server model test verifies that the existing migration snapshot remains consistent.

SQLite tests do not replace SQL Server integration tests for provider-specific behaviour, including collation, precision and concurrency.

## 13.2 API tests

`Enerweb.Api.Tests` validates HTTP behaviour through the real application pipeline.

Coverage includes:

- CRUD operations for each resource.
- Missing records.
- Invalid input.
- Route/body identifier mismatches.
- Business conflicts.
- HTTP response conventions.

API tests use isolated SQLite databases.

## 13.3 Database integration tests

`Enerweb.Data.IntegrationTests` exercises business rules against a real SQL Server instance using an isolated, migrated and disposable database.

The application database must never be used for integration tests.

## 13.4 Validation commands

The following commands should be executed before accepting changes:

```powershell
dotnet restore Enerweb.sln

dotnet build Enerweb.sln --no-restore

dotnet test Enerweb.sln --no-restore

dotnet ef migrations has-pending-model-changes --project Enerweb.Data
```

# 14. Adding New Entities and Capabilities

New entities must follow the established architectural conventions.

The implementation process is:

1. Create the domain entity in `Enerweb.Domain.Models`.
2. Configure the entity in `EnerwebDbContext`, including relationships, constraints and seed data where necessary.
3. Generate and review the database migration.
4. Create a dedicated repository contract inheriting from `IBaseRepository<TEntity>`.
5. Implement the repository using `BaseRepository<TEntity>`.
6. Register the dedicated repository and its generic alias as the same scoped instance.
7. Create the service contract and implementation.
8. Implement business-specific validation and allowed field copying.
9. Register the service through the Services dependency injection module.
10. Create the API controller using the shared CRUD controller where appropriate.
11. Add persistence and HTTP integration tests.

Entity-specific workflows should reuse generic repository functionality rather than duplicating common CRUD operations.

# 15. Consequences of the Architecture

## 15.1 Benefits

The selected architecture provides:

- Clear separation between HTTP, business logic and persistence.
- Reusable CRUD implementations.
- Explicit repository contracts.
- Consistent transaction coordination.
- Improved testability.
- Centralized error handling and logging.
- Consistent API conventions.
- A foundation for more advanced reporting capabilities.
- Flexibility to evolve internal implementations without unnecessarily changing API contracts.

## 15.2 Trade-offs

The architecture introduces additional interfaces, projects and dependency registrations.

Developers must understand the dependency boundaries and transaction responsibilities.

The generic repository and Unit of Work abstractions partially overlap with functionality already provided by EF Core.

This is an intentional trade-off in exchange for explicit persistence boundaries and application-level control.

Abstractions should remain lightweight and should not duplicate framework functionality unnecessarily.

# 16. Future Architectural Considerations

The following areas are intentionally left open and should be addressed when justified by functional requirements.

| Area | Current position |
|---|---|
| Microservices / distributed deployment | Not currently required |
| Reporting execution engine | Not implemented |
| Report execution history | Future consideration |
| Asynchronous report processing | Future consideration |
| Natural-language report generation | Future consideration |
| Report sharing and ownership enforcement | Requires further design |
| Authentication and authorization | Not currently configured |
| Reporting result caching | Not currently implemented |
| Advanced query optimization | To be evaluated as reporting requirements grow |

Future decisions must preserve the established architectural principles while avoiding unnecessary complexity.

---

# 17. Architectural Decision Summary

The Enerweb application adopts a layered architecture with explicit separation of API, service, interface, domain and data responsibilities.

Generic repository functionality is reused through entity-specific repository contracts.

Services own business validation and application workflows.

Unit of Work coordinates persistence across multiple repositories using a shared scoped EF Core context.

Database constraints remain authoritative for data integrity.

Centralized middleware handles HTTP exception responses and request logging.

Reporting is treated as a distinct application capability, with persisted report definitions separated from the future execution engine.

The architecture intentionally avoids committing to a specific future deployment model.

**The guiding architectural decision is to establish clear, maintainable boundaries now while retaining the flexibility to evolve the application as its requirements grow.**

# 18. AI use

## Tools used

- **OpenAI Codex:** GPT 6.1 Sol Light-  Used to inspect the existing solution, draft and revise repository/service contracts, controllers, EF Core configuration and migrations, UTC handling, consumption summaries, request logging, tests and documentation. Changes were guided by the requirements and the existing project structure.
- **PowerShell, the .NET CLI and EF Core CLI:** Used through Codex to inspect files, build projects, generate and review migrations, apply database changes and run the test suites. Microsoft documentation and NuGet package information were consulted for framework and package compatibility.
- **Podman and SQL Server tools:** Used to run the SQL Server container, exercise real database constraints, verify seeded readings, and create and verify a database backup. Podman was used because Docker was not installed on the development machine.
- **Git:** Used to inspect changes, exclude local credentials and generated files, prepare a reviewable commit message, and commit and push after approval.

## Suggestions changed or rejected

The initial architecture used the generic repository directly for every model. This was changed to dedicated repository interfaces and implementations for each model, while retaining the generic base implementation. The change  allowed entity-specific queries to be added without duplicating CRUD logic.

The first summary request also used optional `startDate` and `endDate` fields. These were replaced with required `fromUtc` and `toUtc` fields when the business rules were clarified. The calculation was extended to group positive consecutive-reading differences by the UTC date of the later reading. Overall consumption remains the latest minus the earliest in-range value; it is not calculated by summing cumulative readings or daily positive differences.

## How correctness was checked

- **Builds and automated tests:** The latest verified run passed 64 tests across data, API and SQL Server integration suites. Tests exercise the actual implementations, including dependency injection and the HTTP pipeline. When the running Visual Studio API locked build files, verification used a separate temporary build directory.
- **Real SQL Server checks:** Integration tests create an isolated database, apply the migrations and remove that database afterward. They verify relationships, persisted UTC timestamps and decimal values, duplicate timestamp rejection on create/update, direct database constraint enforcement and competing concurrent writes. The application database is not used for these tests.
- **Summary edge cases:** Tests cover inclusive bounds, out-of-order inserts, multiple UTC days, cross-midnight attribution, zero and negative differences, empty and single-reading ranges, other meters being excluded, invalid ranges and unknown meters.
- **API and logging checks:** Tests verify status codes, UTC ISO 8601 output, offset normalization, optional date filters, one database log per request, linked exception details, cancellation and safe responses when logging fails. Swagger UI and its OpenAPI document were also checked through HTTP requests.
- **Manual review:** Generated migrations were inspected before application, model snapshots were checked for pending changes, and seeded reading counts and ranges were queried in SQL Server. Git ignore rules and staged files were checked to keep credentials, database backups, container archives and build output out of the repository.

Passing tests were used as evidence for the implemented rules, not as proof of production readiness. Authentication, authorization and report execution remain outside the implemented scope.
