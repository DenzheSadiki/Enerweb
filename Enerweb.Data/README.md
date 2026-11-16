# EF Core database

`EnerwebDbContext` maps the domain models to SQL Server. The design-time factory
reads the individual settings in `../Enerweb.Data.Databaase/database.local.json`
and builds a connection string without storing credentials in migrations.

From the repository root, with the SQL Server container running:

```powershell
dotnet tool restore
dotnet ef database update --project Enerweb.Data
```

The initial migration creates Sites, Meters, Readings, Addresses, and ContactPeople,
and inserts demo records. The SeedHourlyReadings migration expands the demo meter
to ten hourly readings on 15 November 2026, from 00:00 to 09:00 UTC, increasing
from 1250 to 1580 kWh. Its summary matches the latest two readings.
The demo meter belongs to the demo site,
and the demo reading belongs to that meter. Address and contact records are
independent because the domain models do not currently define their relationships.
Seed data uses fixed IDs and is applied once through the migration history.

For future model changes:

```powershell
dotnet ef migrations add DescribeYourChange --project Enerweb.Data
dotnet ef database update --project Enerweb.Data
```
