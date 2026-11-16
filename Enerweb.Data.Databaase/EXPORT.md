# Container and database export

Exports are stored in `exports/<UTC timestamp>/` and excluded from Git. Each
completed export contains:

- `sqlserver-container.tar`: the current container filesystem from `podman export`.
- `Enerweb_<UTC timestamp>.bak`: a compressed, copy-only SQL Server backup with
  checksums, verified using `RESTORE VERIFYONLY` before copying to this project.
- `export.json`: the export metadata and original startup commands.

The SQL data resides in a named volume and is not included in the container
filesystem archive. The `.bak` is required to recover the actual database. The
archive is a filesystem export, not a Docker/Podman image-save archive.

## Restore the database

Run from this folder. Start SQL Server using `compose.yaml`, then copy the chosen
backup into the container, replacing the example path with the actual export:

```powershell
podman cp ./exports/<timestamp>/Enerweb_<timestamp>.bak enerweb-database-sqlserver-1:/var/opt/mssql/Enerweb-backup.bak
```

Connect with your SQL client using the local configuration. Inspect the files:

```sql
RESTORE FILELISTONLY FROM DISK = N'/var/opt/mssql/Enerweb-backup.bak';
```

Restore into a separate database to preserve the current application database:

```sql
RESTORE DATABASE [EnerwebRestored]
FROM DISK = N'/var/opt/mssql/Enerweb-backup.bak'
WITH MOVE N'Enerweb' TO N'/var/opt/mssql/data/EnerwebRestored.mdf',
     MOVE N'Enerweb_log' TO N'/var/opt/mssql/data/EnerwebRestored_log.ldf',
     CHECKSUM;
DBCC CHECKDB ([EnerwebRestored]) WITH NO_INFOMSGS;
```

Use the logical names from FILELISTONLY if they differ. No WITH REPLACE is used;
choose another database/file name if the restored database already exists.

## Import the container filesystem

If the exported container filesystem is needed separately:

```powershell
podman import ./exports/<timestamp>/sqlserver-container.tar localhost/enerweb-sqlserver-export:local
```

Import does not preserve container settings, startup commands, environment,
ports, or volumes. Recreate those explicitly from `compose.yaml` and `export.json`,
including entrypoint `/opt/mssql/bin/launch_sqlservr.sh` and command
`/opt/mssql/bin/sqlservr`, then restore the `.bak`. To recreate just the database
with the standard SQL Server image, the existing Compose file and backup suffice.

The export and backup may contain application data; they stay local and are not
part of the source-code submission. Existing containers and data remain running.
