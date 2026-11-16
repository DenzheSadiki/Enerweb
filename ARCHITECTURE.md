# Repository and service architecture

Request/response DTOs live in `Enerweb.Domain.Models/Dtos` with namespace
`Enerweb.Domain.Models.Dtos`: ReadingSummaryRequest, ReadingSummaryDto,
DailyConsumptionDto, and MeterWithReadings. ReportRequest remains a persisted
entity mapped to Reports, alongside the other database entities.

Controllers inject model service contracts for all five domain models. Services
own business validation and compose LINQ queries through repository contracts.
Repositories stage EF changes; services explicitly commit through `IUnitOfWork`.

Project references flow as follows:

```text
Enerweb.Api -> Enerweb.Services -> Enerweb.Interfaces -> Enerweb.Domain.Models
Enerweb.Api -> Enerweb.Data     -> Enerweb.Interfaces
                              -> Enerweb.Domain.Models
Enerweb.Data.Tests -> Enerweb.Data + Enerweb.Services
Enerweb.Api.Tests -> Enerweb.Api (HTTP integration tests)
```

The API references Data only to configure persistence in the composition root.
Controllers must not inject repositories or the context. Services reference EF
Core for async LINQ and Include extensions, but never reference the Data project
or its context. Each model has a dedicated repository interface and implementation
that inherits the shared generic CRUD implementation. Services inject those
dedicated contracts. Generic and dedicated contracts resolve to the same scoped
repository instance for each existing model; the open generic remains available
for additional entities.

## Querying and persistence

`IBaseRepository<TEntity>.Query()` returns a deferred, no-tracking query.
`QueryTracking()` and `GetByIdAsync(Guid)` return tracked entities for updates.
Missing IDs return null; services throw `KeyNotFoundException` for missing writes.
Repository methods never save changes. Null entity arguments are rejected.

All scoped repositories share the scoped context and unit of work. A business
workflow can stage operations in several repositories, then call
`SaveChangesAsync()` once. EF applies that save in one transaction, including
rollback if any operation fails. The initial service CRUD methods each commit
their operation; larger workflows should coordinate repositories and commit once
rather than chaining service CRUD methods and expecting one transaction.
Dispose the DI scope after a failed save or business operation; it is not a
recovery boundary for continuing unrelated writes.

Do not execute parallel database operations on the same scope/context. Execute
queries before the scope is disposed, and never return IQueryable to API clients.

Example of a future reporting query inside a service:

```csharp
var query = readings.Query().Where(x => x.Meter.SiteId == siteId);
if (startDate.HasValue)
    query = query.Where(x => x.Timestamp >= startDate.Value);

var page = await query.OrderByDescending(x => x.Timestamp)
    .ThenBy(x => x.Id)
    .Select(x => new { x.Timestamp, x.Value, x.MeterId })
    .Skip(offset).Take(pageSize).ToListAsync();
```

`Where`, `Include`, projection, sorting, grouping, aggregates, and pagination
remain composable. A future reporting endpoint must authorize accessible data,
allowlist fields/operators, and bound page sizes. Do not execute client SQL or
unvalidated expression strings. No dynamic expression parser is included.

## Initial business rules

- A meter may have only one reading at each UTC timestamp. Service validation
  covers create/update (excluding the current record), and a unique composite
  index enforces the same rule for concurrent or direct repository writes.
  Conflicts return HTTP 409. The same timestamp on different meters is valid.

- Sites require a name and a unique code of at most 50 characters.
- Meters require an existing site, identifying fields, and a unique meter number
  of at most 100 characters. Current reading must be at least the nonnegative
  previous reading; consumption is calculated from their difference.
- Readings require an existing meter, a nonnegative value, and a nonfuture
  timestamp. The reading log is maintained independently of meter summary fields;
  automatic recalculation of summaries for historical edits is not implemented.
- Sites with meters and meters with readings cannot be deleted. This matches
  the existing database restrictions.
- Creates set audit timestamps; updates copy allowed scalar fields onto the
  tracked record, preserving IDs and creation timestamps. Manage child entities
  separately rather than submitting nested creation graphs.

Unique database indexes remain authoritative for concurrent writes. Validation
queries improve error messages but cannot prevent a race; callers must handle
`DbUpdateException` at the API boundary. The error-logging middleware maps argument
errors to 400, missing records to 404, and business/database conflicts to 409.
Database exception details are not returned to clients.

`Meters.SiteId` exposes the existing shadow foreign key as a CLR property.
The database column, relationships, seed data, and migrations are unchanged.
`MeterWithReadings` combines the existing entities without adding a navigation
property or changing the model.

## Configuration and API compatibility

### Error logging

`Enerweb.Middleware` is referenced by the API and depends on Interfaces, ASP.NET
Core, and EF Core's exception type. `UseErrorLogging()` wraps downstream request
handling, replacing the MVC exception filter. Caught validation, missing-record,
conflict, and unexpected exceptions receive 400/404/409/500 ProblemDetails
responses with a trace ID. Unexpected 500 and database errors do not expose
exception details in HTTP responses. Responses that already started are not
rewritten, and client-request cancellation is rethrown without an error record.

`ILogService`/`LogService` stages an `Error` and linked `DatabaseLog` through
`IErrorRepository`/`ErrorRepository` and
`IDatabaseLogRepository`/`DatabaseLogRepository`, then commits both together.
The `AddErrorLogging` migration adds Errors and DatabaseLogs with a foreign key
and timestamp/trace indexes. All logging timestamps are assigned in UTC.

Middleware resolves the log service in a fresh scope, keeping logging separate
from failed or pending request database changes. Persistence has a five-second
cancellation timeout; database logging failures fall back to the application
logger without replacing the original HTTP response or recursively retrying.
Every request produces one DatabaseLog, including successful responses, redirects,
unmatched routes, automatic model-validation failures, and exceptions. Client
cancellation is recorded with status 499 and no exception record. ErrorId is
nullable for requests without an exception. LogRequestAsync assigns Information,
Warning, or Error level based on status; LogErrorAsync links exception details.
The LogAllApiRequests migration makes ErrorId nullable and adds duration in
milliseconds. Only the path, method, status, duration, and trace ID are captured, not
headers, query strings, or request bodies. Exception details are retained in the
database/application logs. Requests without exceptions do not create Error rows.
There are no public CRUD endpoints for the log tables. Logging occurs after
downstream request handling and excludes the database logging duration itself.

All DateTime timestamps are UTC. EF normalizes local values on write and restores
DateTimeKind.Utc on read, including nullable timestamps; existing timezone-free
SQL datetime2 values are interpreted as UTC. The API accepts ISO 8601 timestamps,
normalizes explicit offsets to UTC, and treats timestamps without an offset as
UTC. Responses use the ISO 8601 round-trip format, for example
`2026-11-16T10:30:00.0000000Z`. Null timestamps remain JSON null. Date-only values
are unaffected. Database columns remain datetime2; strings are the JSON format.

In Development, the API reads individual `Database` settings from
`Enerweb.Data.Databaase/database.local.json`. User secrets, environment variables,
and command-line arguments can override those settings. For deployments, supply
`ConnectionStrings__DefaultConnection`, or `Database__Host`, `Database__Port`,
`Database__Name`, `Database__Username`, `Database__Password`, `Database__Encrypt`,
and `Database__TrustServerCertificate`. Local credentials are not copied into
build output. Database migrations are an explicit deployment step, not an
automatic operation on API startup.

The existing API targeted .NET 8 while using .NET 9 OpenAPI APIs. Swashbuckle now
provides the Development OpenAPI document at the same `/openapi/v1.json` route.
The existing weather controller is retained.

## Adding an entity and service

1. Add a domain entity, for example `Tariff` with a `Guid Id`, in Domain.Models.
2. Map it in `EnerwebDbContext` (including precision, constraints, and any seed
   data), generate a migration, review it, and apply it explicitly.
3. Add `ITariffRepository : IBaseRepository<Tariff>` and
   `TariffRepository : BaseRepository<Tariff>, ITariffRepository`. Register the
   dedicated contract and alias `IBaseRepository<Tariff>` to the same scoped
   instance in Data/DependencyInjection.cs. Inherit CRUD rather than copying it.
4. Add `ITariffService : IBaseService<Tariff>` under Interfaces/Services. Add
   business-specific operations there when necessary.
5. Implement `TariffService : BaseService<Tariff>, ITariffService`. Provide the ID
   selector, business validation, and allowed field copying. Override creation
   and deletion validation only for entity-specific rules. Inject other generic
   repositories for workflows, stage their changes, and commit once.
6. Register the service with `AddScoped<ITariffService, TariffService>()` in
   Services/DependencyInjection.cs. Inject only that contract into a controller.
7. Add relational tests to `Enerweb.Data.Tests`.
8. Add a controller inheriting `CrudController<Tariff>`, injecting `ITariffService`,
   with ID getter/setter overrides. Add HTTP tests to `Enerweb.Api.Tests`.

## HTTP CRUD endpoints

`GET /api/Readings/meter/{meterId}` (`GetMeterReadingsByMeterId`) returns that
meter's readings newest first. Optional `fromUtc` and `toUtc` query parameters
filter timestamps inclusively; either can be omitted. ISO 8601 offsets are
normalized to UTC. Invalid dates or reversed ranges return 400; no matching
readings returns an empty list.

`POST /api/Readings/summary` accepts a `ReadingSummaryRequest` containing `meterId`
and required inclusive `fromUtc`/`toUtc` UTC timestamps. It returns a
`ReadingSummaryDto` with count, first/last timestamps and values, minimum,
maximum, average, and consumption (last cumulative reading minus first).
No readings returns count/consumption zero and null statistics/timestamps; one
reading returns zero consumption. DailyBreakdown orders only the in-range
readings by timestamp and groups each consecutive pair's positive difference by
the UTC date of its later reading. Zero/negative differences contribute zero;
dates without an attributed pair are omitted. Total consumption remains the
last minus first reading and may differ from the sum of daily positive deltas.
Unknown meters return 404 and missing or invalid ranges
return 400. Results use the same UTC ISO 8601 serialization as other endpoints.

All five resources expose the same operations:

| Resource | Base route |
| --- | --- |
| Sites | `/api/Sites` |
| Meters | `/api/Meters` |
| Readings | `/api/Readings` |
| Addresses | `/api/Addresses` |
| Contact people | `/api/ContactPeople` |
| Reports | `/api/Reports` |

Use GET on the base route to list records, GET `/{id}` for one record, POST on the
base route to create, PUT `/{id}` to update, and DELETE `/{id}` to delete. IDs are
GUIDs. Create returns 201 and a Location header; update/delete return 204. Missing
records return 404, invalid input returns 400, and business conflicts return 409.
The shared abstract controller contains the HTTP CRUD flow; concrete controllers
only select the service and model ID property (including Address's existing `id`).

POST/PUT bodies contain scalar fields. Supply `siteId` for a meter and `meterId`
for a reading; navigation objects are not required. IDs can be omitted on create
and update; update assigns the route ID. If a body ID is supplied on update, it
must match the route. Lists currently return all records. These initial endpoints
inherit the application's current access policy; no authentication is configured.

Address and contact services validate their required fields; contacts also require
a valid email address. Their create/update audit fields are managed by the service.

Reports use `ReportRequest`, `IReportRepository`/`ReportRepository`, and
`IReportService`/`ReportService`. The `Reports` table is added by the `AddReports`
migration. Report names (up to 200 characters), entity names (up to 100 characters),
query definitions, and a nonempty RequestedBy ID are required. Create assigns a
UTC creation time and clears LastExecutedAt; updates preserve ownership, creation
time, and execution history. QueryDefinition is stored as text, never executed.
Report execution and authenticated ownership enforcement are not implemented.

## Validation

`Enerweb.Data.IntegrationTests` exercises these business rules against real SQL
Server in an isolated, migrated, disposable database. See its README for local
and CI configuration. The application database is never used for integration tests.

```powershell
dotnet restore Enerweb.sln
dotnet build Enerweb.sln --no-restore
dotnet test Enerweb.sln --no-restore
dotnet ef migrations has-pending-model-changes --project Enerweb.Data
```

Tests use isolated SQLite databases with foreign keys enabled, including
multi-repository rollback. A SQL Server model test verifies that the existing
migration snapshot still matches. SQLite tests do not replace SQL Server
integration tests for provider-specific collation, precision, or concurrency.
API tests exercise CRUD for each model, missing records, validation, route/body
ID mismatches, and conflicts through the real HTTP pipeline using isolated SQLite.

## Files changed for this architecture

The per-model repository and controller additions include dedicated repository
interfaces under `Enerweb.Interfaces/Repositories` and implementations under
`Enerweb.Data/Repositories` for Site, Meter, Reading, Address, and ContactPerson.
Address and contact service contracts/implementations were added, and existing
services now inject the dedicated repository contracts. Both DI modules were
updated, including aliases that preserve shared scoped repository instances.

The API adds `CrudController`, concrete resource controllers, and
JSON/model-binding configuration in Program. `Enerweb.Middleware` now handles
exceptions and database logging in place of the former `ApiExceptionFilter`.
`Enerweb.Api.Tests/CrudControllerTests.cs` covers HTTP behavior; its project now
references the API, ASP.NET Core testing, and SQLite packages. The existing data
tests also verify the dedicated repository aliases and additional services.

Created:

- `Enerweb.Interfaces/Repositories/IBaseRepository.cs`
- `Enerweb.Interfaces/IUnitOfWork.cs`
- `Enerweb.Interfaces/Services/ISiteService.cs`
- `Enerweb.Interfaces/Services/IMeterService.cs` (includes `MeterWithReadings`)
- `Enerweb.Interfaces/Services/IReadingService.cs`
- `Enerweb.Data/Repositories/BaseRepository.cs`
- `Enerweb.Data/UnitOfWork.cs`
- `Enerweb.Data/DependencyInjection.cs`
- `Enerweb.Services/BaseService.cs`
- `Enerweb.Services/SiteService.cs`
- `Enerweb.Services/MeterService.cs`
- `Enerweb.Services/ReadingService.cs`
- `Enerweb.Services/DependencyInjection.cs`
- `Enerweb.Data.Tests/ArchitectureTests.cs`
- `Enerweb.Data.Tests/Enerweb.Data.Tests.csproj`
- `ARCHITECTURE.md`

Modified: all five project files for Interfaces, Data, Services, API, and Tests;
`Enerweb.sln`; `Enerweb/Program.cs`; `Enerweb.Domain.Models/Meters.cs`;
and `Enerweb.Data/EnerwebDbContext.cs`.

Removed: the superseded `IBaseRepository`, `ISiteRepository`, and
`IMeterRepository` contracts under `Enerweb.Data/Repository`. Their retrieval
operations now live on service contracts; CRUD uses the shared generic repository.
