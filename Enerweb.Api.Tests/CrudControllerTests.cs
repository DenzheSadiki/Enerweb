using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Enerweb.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Enerweb.Api.Tests;

public sealed class CrudControllerTests
{
    [Fact]
    public async Task EachSuccessfulMissingAndValidationRequestCreatesExactlyOneDatabaseLog()
    {
        using var app = new TestApi();
        using var client = app.CreateDatabaseClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/Sites?token=private")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/not-a-route")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync("/api/Readings/meter/22222222-2222-2222-2222-222222222222?fromUtc=invalid")).StatusCode);
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnerwebDbContext>();
        // A successful response can flush before the middleware's finally block completes.
        for (var attempt = 0; attempt < 50 && await context.DatabaseLogs.CountAsync() < 3; attempt++)
            await Task.Delay(10);
        var logs = await context.DatabaseLogs.AsNoTracking().ToListAsync();
        Assert.Equal(3, logs.Count);
        Assert.Equal(3, logs.Select(log => log.TraceId).Distinct().Count());
        Assert.Empty(await context.Errors.ToListAsync());
        Assert.Contains(logs, log => log.RequestPath == "/api/Sites" && log.StatusCode == 200 && log.Level == "Information");
        Assert.Contains(logs, log => log.RequestPath == "/api/not-a-route" && log.StatusCode == 404 && log.Level == "Warning");
        Assert.Contains(logs, log => log.StatusCode == 400);
        Assert.All(logs, log =>
        {
            Assert.Null(log.ErrorId);
            Assert.Equal("GET", log.HttpMethod);
            Assert.Equal(DateTimeKind.Utc, log.Timestamp.Kind);
            Assert.True(log.DurationMilliseconds >= 0);
            Assert.DoesNotContain("?", log.RequestPath);
        });
    }

    [Fact]
    public async Task DuplicateUtcTimestampsReturnConflictOnCreateAndUpdate()
    {
        using var app = new TestApi();
        using var client = app.CreateDatabaseClient();
        var body = CreateBody("Readings");
        body["timestamp"] = "2020-01-02T12:00:00+02:00";
        var created = await client.PostAsJsonAsync("/api/Readings", body);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        body["timestamp"] = "2020-01-02T10:00:00Z";
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/Readings", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PutAsJsonAsync("/api/Readings/33333333-3333-3333-3333-333333333333", body)).StatusCode);
    }

    [Theory]
    [InlineData("", 10)]
    [InlineData("?fromUtc=2026-11-15T02:00:00Z", 8)]
    [InlineData("?toUtc=2026-11-15T05:00:00Z", 6)]
    [InlineData("?fromUtc=2026-11-15T02:00:00Z&toUtc=2026-11-15T05:00:00Z", 4)]
    [InlineData("?fromUtc=2026-11-15T02:00:00&toUtc=2026-11-15T05:00:00", 4)]
    [InlineData("?fromUtc=2026-11-15T04:00:00%2B02:00&toUtc=2026-11-15T07:00:00%2B02:00", 4)]
    [InlineData("?fromUtc=2026-11-15T02:00:00Z&toUtc=2026-11-15T02:00:00Z", 1)]
    [InlineData("?toUtc=2020-01-01T00:00:00Z", 0)]
    public async Task GetMeterReadingsSupportsOptionalInclusiveUtcFiltering(string query, int expectedCount)
    {
        using var app = new TestApi();
        using var client = app.CreateDatabaseClient();
        var response = await client.GetAsync("/api/Readings/meter/22222222-2222-2222-2222-222222222222" + query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var readings = (await response.Content.ReadFromJsonAsync<JsonArray>())!;
        Assert.Equal(expectedCount, readings.Count);
        var timestamps = readings.Select(reading => DateTimeOffset.Parse(reading!["timestamp"]!.GetValue<string>())).ToList();
        Assert.Equal(timestamps.OrderByDescending(timestamp => timestamp), timestamps);
        Assert.All(readings, reading => Assert.Equal("22222222-2222-2222-2222-222222222222", reading!["meterId"]!.GetValue<string>()));
    }

    [Fact]
    public async Task GetMeterReadingsRejectsInvalidDateFilters()
    {
        using var app = new TestApi();
        using var client = app.CreateDatabaseClient();
        const string path = "/api/Readings/meter/22222222-2222-2222-2222-222222222222";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(path + "?fromUtc=2026-11-16T00:00:00Z&toUtc=2026-11-15T00:00:00Z")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(path + "?fromUtc=invalid")).StatusCode);
    }

    [Fact]
    public async Task RequestExceptionsPersistLinkedErrorAndDatabaseLog()
    {
        using var app = new TestApi();
        using var client = app.CreateDatabaseClient();
        var response = await client.PostAsJsonAsync("/api/Sites?token=not-logged", new { });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnerwebDbContext>();
        var log = await context.DatabaseLogs.SingleAsync();
        var error = await context.Errors.SingleAsync();
        Assert.Equal(error.Id, log.ErrorId);
        Assert.Equal(400, log.StatusCode);
        Assert.Equal("POST", log.HttpMethod);
        Assert.Equal("/api/Sites", log.RequestPath);
        Assert.Equal(problem["traceId"]!.GetValue<string>(), log.TraceId);
        Assert.Equal(DateTimeKind.Utc, log.Timestamp.Kind);
        Assert.Equal("System.ArgumentException", error.ExceptionType);
    }

    [Fact]
    public async Task ReadingSummaryEndpointReturnsStatisticsAndUtcTimestamps()
    {
        using var app = new TestApi();
        using var client = app.CreateDatabaseClient();
        var response = await client.PostAsJsonAsync("/api/Readings/summary", new
        {
            meterId = "22222222-2222-2222-2222-222222222222",
            fromUtc = "2026-11-15T04:00:00+02:00",
            toUtc = "2026-11-15T07:00:00+02:00"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        Assert.Equal(4, summary["readingCount"]!.GetValue<int>());
        Assert.Equal(98m, summary["consumption"]!.GetValue<decimal>());
        var daily = Assert.Single(summary["dailyBreakdown"]!.AsArray());
        Assert.Equal("2026-11-15", daily!["date"]!.GetValue<string>());
        Assert.Equal(98m, daily["consumption"]!.GetValue<decimal>());
        Assert.Equal("2026-11-15T02:00:00.0000000Z", summary["firstTimestamp"]!.GetValue<string>());
        Assert.Equal("2026-11-15T05:00:00.0000000Z", summary["lastTimestamp"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/Readings/summary", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/Readings/summary", new { meterId = "22222222-2222-2222-2222-222222222222" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PostAsJsonAsync("/api/Readings/summary", new { meterId = Guid.NewGuid(), fromUtc = "2026-11-15T00:00:00Z", toUtc = "2026-11-15T09:00:00Z" })).StatusCode);
    }

    [Theory]
    [InlineData("2020-01-02T12:30:00+02:00", "2020-01-02T10:30:00.0000000Z")]
    [InlineData("2020-01-02T10:30:00Z", "2020-01-02T10:30:00.0000000Z")]
    [InlineData("2020-01-02T10:30:00", "2020-01-02T10:30:00.0000000Z")]
    public async Task TimestampsAreNormalisedToUtcAndRemainIso8601AfterReload(string input, string expected)
    {
        using var app = new TestApi();
        using var client = app.CreateDatabaseClient();
        var body = CreateBody("Readings");
        body["timestamp"] = input;
        var response = await client.PostAsJsonAsync("/api/Readings", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        Assert.Equal(expected, created["timestamp"]!.GetValue<string>());
        var reloaded = (await client.GetFromJsonAsync<JsonObject>(response.Headers.Location))!;
        Assert.Equal(expected, reloaded["timestamp"]!.GetValue<string>());
    }

    [Fact]
    public async Task NullableTimestampsUseUtcIso8601AndPreserveNull()
    {
        using var app = new TestApi();
        using var client = app.CreateDatabaseClient();
        var body = CreateBody("Meters");
        body["installationDate"] = "2020-01-02T12:30:00+02:00";
        var response = await client.PostAsJsonAsync("/api/Meters", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var reloaded = (await client.GetFromJsonAsync<JsonObject>(response.Headers.Location))!;
        Assert.Equal("2020-01-02T10:30:00.0000000Z", reloaded["installationDate"]!.GetValue<string>());
        Assert.Null(reloaded["lastReadingDate"]);
        Assert.EndsWith("Z", reloaded["createdAt"]!.GetValue<string>());
        Assert.Null(reloaded["updatedAt"]);
    }

    [Theory]
    [InlineData("Sites", "siteName", "Changed Site")]
    [InlineData("Meters", "meterName", "Changed Meter")]
    [InlineData("Readings", "value", "1400")]
    [InlineData("Addresses", "city", "Cape Town")]
    [InlineData("ContactPeople", "name", "Changed Contact")]
    [InlineData("Reports", "name", "Changed Report")]
    public async Task EveryModelSupportsCreateReadUpdateAndDelete(string resource, string field, string newValue)
    {
        using var app = new TestApi();
        using var client = app.CreateDatabaseClient();
        var path = $"/api/{resource}";
        var body = CreateBody(resource);
        var created = await client.PostAsJsonAsync(path, body);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);
        var entity = (await created.Content.ReadFromJsonAsync<JsonObject>())!;
        var id = entity["id"]!.GetValue<Guid>();
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(created.Headers.Location)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);

        // Only post scalar fields: nested relationships are managed separately.
        body["id"] = JsonValue.Create(id);
        body[field] = resource == "Readings" ? JsonValue.Create(1400m) : JsonValue.Create(newValue);
        var updated = await client.PutAsJsonAsync($"{path}/{id}", body);
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
        var fetched = (await client.GetFromJsonAsync<JsonObject>($"{path}/{id}"))!;
        if (resource == "Readings")
            Assert.Equal(1400m, fetched[field]!.GetValue<decimal>());
        else
            Assert.Equal(newValue, fetched[field]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"{path}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{path}/{id}")).StatusCode);
    }

    [Theory]
    [InlineData("Sites")]
    [InlineData("Meters")]
    [InlineData("Readings")]
    [InlineData("Addresses")]
    [InlineData("ContactPeople")]
    [InlineData("Reports")]
    public async Task MissingRecordsReturnNotFound(string resource)
    {
        using var app = new TestApi();
        using var client = app.CreateDatabaseClient();
        var path = $"/api/{resource}/{Guid.NewGuid()}";
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync(path, CreateBody(resource))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync(path)).StatusCode);
    }

    [Fact]
    public async Task ValidationIdMismatchAndConflictsReturnUsefulStatusCodes()
    {
        using var app = new TestApi();
        using var client = app.CreateDatabaseClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Sites", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Reports", new { })).StatusCode);
        var report = CreateBody("Reports");
        report["requestedBy"] = JsonValue.Create(Guid.Empty);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Reports", report)).StatusCode);
        report = CreateBody("Reports");
        report["queryDefinition"] = " ";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Reports", report)).StatusCode);
        var mismatch = CreateBody("Sites");
        mismatch["id"] = JsonValue.Create(Guid.NewGuid());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/Sites/{Guid.NewGuid()}", mismatch)).StatusCode);
        var duplicate = CreateBody("Sites");
        duplicate["siteCode"] = "DEMO-001";
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/Sites", duplicate)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.DeleteAsync("/api/Sites/11111111-1111-1111-1111-111111111111")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.DeleteAsync("/api/Meters/22222222-2222-2222-2222-222222222222")).StatusCode);
        var contact = CreateBody("ContactPeople");
        contact["email"] = "invalid";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/ContactPeople", contact)).StatusCode);
    }

    private static JsonObject CreateBody(string resource) => resource switch
    {
        "Sites" => new() { ["siteName"] = "API Site", ["siteCode"] = Guid.NewGuid().ToString("N"), ["isActive"] = true },
        "Reports" => new()
        {
            ["name"] = "Meter report", ["entity"] = "Meters", ["queryDefinition"] = "{}",
            ["requestedBy"] = JsonValue.Create(Guid.NewGuid()), ["isSaved"] = true
        },
        "Meters" => new()
        {
            ["siteId"] = "11111111-1111-1111-1111-111111111111",
            ["meterNumber"] = Guid.NewGuid().ToString("N"), ["serialNumber"] = "API-SN",
            ["meterName"] = "API Meter", ["meterType"] = "Electricity", ["meterCategory"] = "Postpaid",
            ["unitOfMeasurement"] = "kWh", ["currentReading"] = 100, ["previousReading"] = 50,
            ["propertyId"] = "API-PROP", ["accountNumber"] = "API-ACC", ["status"] = "Active", ["isActive"] = true
        },
        "Readings" => new()
        {
            ["meterId"] = "22222222-2222-2222-2222-222222222222", ["value"] = 1300,
            ["timestamp"] = JsonValue.Create(DateTime.UtcNow.AddMinutes(-1))
        },
        "Addresses" => new()
        {
            ["addressLine1"] = "1 API Street", ["city"] = "Johannesburg", ["province"] = "Gauteng",
            ["postalCode"] = "2000", ["country"] = "South Africa"
        },
        "ContactPeople" => new() { ["name"] = "API Contact", ["contactNumber"] = "0123456789", ["email"] = "api@example.com" },
        _ => throw new ArgumentException("Unknown resource.")
    };

    private sealed class TestApi : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:;Foreign Keys=True");

        public TestApi() => connection.Open();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<EnerwebDbContext>();
                services.AddScoped(_ => new EnerwebDbContext(
                    new DbContextOptionsBuilder<EnerwebDbContext>().UseSqlite(connection).Options));
            });
        }

        public HttpClient CreateDatabaseClient()
        {
            var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            using var scope = Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<EnerwebDbContext>().Database.EnsureCreated();
            return client;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) connection.Dispose();
        }
    }
}
