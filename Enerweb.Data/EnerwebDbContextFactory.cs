using System;
using System.IO;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Enerweb.Data;

public sealed class EnerwebDbContextFactory : IDesignTimeDbContextFactory<EnerwebDbContext>
{
    public EnerwebDbContext CreateDbContext(string[] args)
    {
        var configPath = FindConfiguration();
        using var document = JsonDocument.Parse(File.ReadAllText(configPath));
        var database = document.RootElement.GetProperty("Database");
        var connection = new SqlConnectionStringBuilder
        {
            DataSource = $"{database.GetProperty("Host").GetString()},{database.GetProperty("Port").GetInt32()}",
            InitialCatalog = database.GetProperty("Name").GetString(),
            UserID = database.GetProperty("Username").GetString(),
            Password = database.GetProperty("Password").GetString(),
            Encrypt = database.GetProperty("Encrypt").GetBoolean(),
            TrustServerCertificate = database.GetProperty("TrustServerCertificate").GetBoolean()
        };

        var options = new DbContextOptionsBuilder<EnerwebDbContext>()
            .UseSqlServer(connection.ConnectionString).Options;
        return new EnerwebDbContext(options);
    }

    private static string FindConfiguration()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
             directory != null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "Enerweb.Data.Databaase", "database.local.json");
            if (File.Exists(path)) return path;
        }

        throw new FileNotFoundException(
            "Run EF commands from the repository root or Enerweb.Data, with Enerweb.Data.Databaase/database.local.json configured.");
    }
}
