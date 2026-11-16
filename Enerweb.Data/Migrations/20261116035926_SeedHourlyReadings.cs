using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Enerweb.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedHourlyReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Meters",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "Consumption", "CurrentReading", "LastReadingDate", "PreviousReading" },
                values: new object[] { 46m, 1580m, new DateTime(2026, 11, 15, 9, 0, 0, 0, DateTimeKind.Utc), 1534m });

            migrationBuilder.InsertData(
                table: "Readings",
                columns: new[] { "Id", "MeterId", "Timestamp", "Value" },
                values: new object[,]
                {
                    { new Guid("33333333-3333-3333-3333-000000000001"), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 11, 15, 1, 0, 0, 0, DateTimeKind.Utc), 1278m },
                    { new Guid("33333333-3333-3333-3333-000000000002"), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 11, 15, 2, 0, 0, 0, DateTimeKind.Utc), 1312m },
                    { new Guid("33333333-3333-3333-3333-000000000003"), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 11, 15, 3, 0, 0, 0, DateTimeKind.Utc), 1341m },
                    { new Guid("33333333-3333-3333-3333-000000000004"), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 11, 15, 4, 0, 0, 0, DateTimeKind.Utc), 1383m },
                    { new Guid("33333333-3333-3333-3333-000000000005"), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 11, 15, 5, 0, 0, 0, DateTimeKind.Utc), 1410m },
                    { new Guid("33333333-3333-3333-3333-000000000006"), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 11, 15, 6, 0, 0, 0, DateTimeKind.Utc), 1456m },
                    { new Guid("33333333-3333-3333-3333-000000000007"), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 11, 15, 7, 0, 0, 0, DateTimeKind.Utc), 1489m },
                    { new Guid("33333333-3333-3333-3333-000000000008"), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 11, 15, 8, 0, 0, 0, DateTimeKind.Utc), 1534m },
                    { new Guid("33333333-3333-3333-3333-000000000009"), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 11, 15, 9, 0, 0, 0, DateTimeKind.Utc), 1580m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Readings",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-000000000001"));

            migrationBuilder.DeleteData(
                table: "Readings",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-000000000002"));

            migrationBuilder.DeleteData(
                table: "Readings",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-000000000003"));

            migrationBuilder.DeleteData(
                table: "Readings",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-000000000004"));

            migrationBuilder.DeleteData(
                table: "Readings",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-000000000005"));

            migrationBuilder.DeleteData(
                table: "Readings",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-000000000006"));

            migrationBuilder.DeleteData(
                table: "Readings",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-000000000007"));

            migrationBuilder.DeleteData(
                table: "Readings",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-000000000008"));

            migrationBuilder.DeleteData(
                table: "Readings",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-000000000009"));

            migrationBuilder.UpdateData(
                table: "Meters",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "Consumption", "CurrentReading", "LastReadingDate", "PreviousReading" },
                values: new object[] { 50m, 1250m, new DateTime(2026, 11, 15, 0, 0, 0, 0, DateTimeKind.Utc), 1200m });
        }
    }
}
