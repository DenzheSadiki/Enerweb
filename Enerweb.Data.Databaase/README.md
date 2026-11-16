# Local SQL Server

SQL Server 2022 Developer for local development, with a persistent data volume
and a health check. The initialization service creates the `Enerweb` database
if it does not exist. The SQL port is exposed only on localhost.

`.env` contains the local administrator password and port. `database.local.json`
contains individual host, port, database name, username, password, and TLS settings. Both are ignored
by Git. Example files can be committed; replace their password placeholders when
setting up another machine. Keep the password and port consistent in both files.
The EF Core design-time factory in `Enerweb.Data` loads this JSON for migrations.
See `../Enerweb.Data/README.md` for schema initialization and seed commands.

Run from this directory with Docker Desktop running in Linux container mode:

```powershell
docker compose up -d --wait
docker compose ps -a
```

Podman with a working machine and Compose provider can use the same file:

```powershell
podman compose up -d --wait
podman compose ps -a
```

On this machine, the standalone Compose provider was downloaded to the temporary
directory. To reuse it in a new PowerShell session, run this before Podman Compose:

```powershell
$env:PODMAN_COMPOSE_PROVIDER = Join-Path $env:TEMP 'enerweb-docker-compose.exe'
```

If the temporary file has been cleaned up, install Docker Compose and set
`PODMAN_COMPOSE_PROVIDER` to its executable path.

Connect to `127.0.0.1,1433`, database `Enerweb`, user `sa`, using the password in
`.env`. Build the application's connection string from the individual `Database`
settings in `database.local.json` using `SqlConnectionStringBuilder`.
For another service on this Compose network, use `sqlserver,1433` as the server.
TrustServerCertificate is enabled for local development's self-signed certificate.

`docker compose down` stops the services and preserves the database volume.
Changing the environment password does not change the password in an existing
database volume; change it through SQL Server as well.
