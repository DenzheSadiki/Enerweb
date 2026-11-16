using Enerweb.Data;
using Enerweb.Services;
using Enerweb.Middleware;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Local credentials stay outside tracked application settings. Environment overrides win.
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile(
        Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath,
            "..", "Enerweb.Data.Databaase", "database.local.json")), optional: true);
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

builder.Services.AddEnerwebData(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        var database = builder.Configuration.GetRequiredSection("Database");
        connectionString = new SqlConnectionStringBuilder
        {
            DataSource = $"{database["Host"]},{database.GetValue<int>("Port")}",
            InitialCatalog = database["Name"],
            UserID = database["Username"],
            Password = database["Password"],
            Encrypt = database.GetValue("Encrypt", true),
            TrustServerCertificate = database.GetValue("TrustServerCertificate", false)
        }.ConnectionString;
    }
    options.UseSqlServer(connectionString);
});
builder.Services.AddEnerwebServices();

// Add services to the container.

builder.Services.AddControllers(options =>
{
    // Services validate scalar fields; navigation properties need not be posted.
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.Converters.Add(new Enerweb.Serialization.UtcDateTimeJsonConverter());
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseErrorLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options => options.RouteTemplate = "openapi/{documentName}.json");
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("../openapi/v1.json", "Enerweb API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
