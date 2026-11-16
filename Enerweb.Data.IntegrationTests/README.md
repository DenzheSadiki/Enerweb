# SQL Server data integration tests

These tests use the real repositories, services, unit of work, EF migrations,
and SQL Server provider. Start the local SQL Server container before running:

```powershell
dotnet test Enerweb.Data.IntegrationTests/Enerweb.Data.IntegrationTests.csproj
```

Connection settings default to `Enerweb.Data.Databaase/database.local.json`.
For CI, set `ENERWEB_TEST_SQL_CONNECTION` to a SQL Server connection string with
permission to create/drop a test database. Credentials are never printed.

Each fixture generates an `EnerwebIntegration_<guid>` database, applies all
migrations, and deletes only that database on cleanup. The application database
is never used, even if supplied in the connection string. A forced process exit
may leave a database with this prefix; it can be removed separately after checking
that no test run is using it. Missing SQL Server fails tests rather than skipping.

Coverage includes one site with multiple meters, multiple UTC cumulative kWh
readings per meter, inclusive consumption summaries across multiple UTC days,
positive daily differences, resets, empty/single-reading ranges, duplicate create
and update rejection, same timestamps on different meters, direct database
constraint enforcement, and competing concurrent commits.

The single uniqueness rule is `(MeterId, Timestamp)` after UTC normalization.
Services reject existing duplicates with InvalidOperationException; the unique
SQL index rejects concurrent or repository-level bypasses with DbUpdateException.
Both become HTTP 409 through the error middleware. Updating a reading without
changing its timestamp is valid. Values do not participate in uniqueness.

The uniqueness migration does not delete or merge existing data. If an existing
application database contains duplicate meter/timestamp pairs, resolve those
records deliberately before applying the migration.
