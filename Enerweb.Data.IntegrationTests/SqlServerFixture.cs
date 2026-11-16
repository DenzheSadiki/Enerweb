using System.Runtime.CompilerServices;
using System.Text.Json;
using Enerweb.Data;
using Enerweb.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Enerweb.Data.IntegrationTests;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly string databaseName = "EnerwebIntegration_" + Guid.NewGuid().ToString("N");
    public ServiceProvider Provider { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var sourceConnection = Environment.GetEnvironmentVariable("ENERWEB_TEST_SQL_CONNECTION");
        if (string.IsNullOrWhiteSpace(sourceConnection))
        {
            using var config = JsonDocument.Parse(File.ReadAllText(ConfigurationPath()));
            var database = config.RootElement.GetProperty("Database");
            sourceConnection = new SqlConnectionStringBuilder
            {
                DataSource = $"{database.GetProperty("Host").GetString()},{database.GetProperty("Port").GetInt32()}",
                UserID = database.GetProperty("Username").GetString(),
                Password = database.GetProperty("Password").GetString(),
                Encrypt = database.GetProperty("Encrypt").GetBoolean(),
                TrustServerCertificate = database.GetProperty("TrustServerCertificate").GetBoolean()
            }.ConnectionString;
        }
        // Always replace the database name: never run integration tests against application data.
        var connection = new SqlConnectionStringBuilder(sourceConnection) { InitialCatalog = databaseName };
        var services = new ServiceCollection();
        services.AddEnerwebData(options => options.UseSqlServer(connection.ConnectionString));
        services.AddEnerwebServices();
        Provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        try
        {
            using var scope = Provider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<EnerwebDbContext>().Database.MigrateAsync();
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        if (Provider == null) return;
        try
        {
            using var scope = Provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<EnerwebDbContext>();
            var actualName = new SqlConnectionStringBuilder(context.Database.GetConnectionString()).InitialCatalog;
            if (actualName != databaseName || !actualName.StartsWith("EnerwebIntegration_", StringComparison.Ordinal))
                throw new InvalidOperationException("Refusing to delete a non-test database.");
            await context.Database.EnsureDeletedAsync();
        }
        finally
        {
            await Provider.DisposeAsync();
            Provider = null!;
        }
    }

    private static string ConfigurationPath([CallerFilePath] string sourceFile = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile)!, "..",
            "Enerweb.Data.Databaase", "database.local.json"));
}
