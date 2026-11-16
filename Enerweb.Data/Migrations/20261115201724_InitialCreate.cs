using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enerweb.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Addresses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Province = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ContactPeople",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactPeople", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SiteCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Meters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeterNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MeterName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MeterType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MeterCategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitOfMeasurement = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentReading = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PreviousReading = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Consumption = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    InstallationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastReadingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PropertyId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Meters_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Readings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Readings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Readings_Meters_MeterId",
                        column: x => x.MeterId,
                        principalTable: "Meters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Addresses",
                columns: new[] { "id", "AddressLine1", "AddressLine2", "City", "Country", "CreatedAt", "PostalCode", "Province", "UpdatedAt" },
                values: new object[] { new Guid("44444444-4444-4444-4444-444444444444"), "1 Demo Street", "", "Johannesburg", "South Africa", new DateTime(2026, 11, 15, 0, 0, 0, 0, DateTimeKind.Utc), "2000", "Gauteng", null });

            migrationBuilder.InsertData(
                table: "ContactPeople",
                columns: new[] { "Id", "ContactNumber", "CreatedAt", "Email", "Name", "UpdatedAt" },
                values: new object[] { new Guid("55555555-5555-5555-5555-555555555555"), "0000000000", new DateTime(2026, 11, 15, 0, 0, 0, 0, DateTimeKind.Utc), "demo@example.com", "Demo Contact", null });

            migrationBuilder.InsertData(
                table: "Sites",
                columns: new[] { "Id", "CreatedAt", "IsActive", "SiteCode", "SiteName", "UpdatedAt" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 11, 15, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEMO-001", "Demo Site", null });

            migrationBuilder.InsertData(
                table: "Meters",
                columns: new[] { "Id", "AccountNumber", "Consumption", "CreatedAt", "CurrentReading", "InstallationDate", "IsActive", "LastReadingDate", "MeterCategory", "MeterName", "MeterNumber", "MeterType", "PreviousReading", "PropertyId", "SerialNumber", "SiteId", "Status", "UnitOfMeasurement", "UpdatedAt" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), "DEMO-ACC-001", 50m, new DateTime(2026, 11, 15, 0, 0, 0, 0, DateTimeKind.Utc), 1250m, new DateTime(2026, 11, 15, 0, 0, 0, 0, DateTimeKind.Utc), true, new DateTime(2026, 11, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Postpaid", "Demo Electricity Meter", "DEMO-MTR-001", "Electricity", 1200m, "DEMO-PROP-001", "DEMO-SN-001", new Guid("11111111-1111-1111-1111-111111111111"), "Active", "kWh", null });

            migrationBuilder.InsertData(
                table: "Readings",
                columns: new[] { "Id", "MeterId", "Timestamp", "Value" },
                values: new object[] { new Guid("33333333-3333-3333-3333-333333333333"), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 11, 15, 0, 0, 0, 0, DateTimeKind.Utc), 1250m });

            migrationBuilder.CreateIndex(
                name: "IX_Meters_MeterNumber",
                table: "Meters",
                column: "MeterNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Meters_SiteId",
                table: "Meters",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Readings_MeterId_Timestamp",
                table: "Readings",
                columns: new[] { "MeterId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Sites_SiteCode",
                table: "Sites",
                column: "SiteCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Addresses");

            migrationBuilder.DropTable(
                name: "ContactPeople");

            migrationBuilder.DropTable(
                name: "Readings");

            migrationBuilder.DropTable(
                name: "Meters");

            migrationBuilder.DropTable(
                name: "Sites");
        }
    }
}
