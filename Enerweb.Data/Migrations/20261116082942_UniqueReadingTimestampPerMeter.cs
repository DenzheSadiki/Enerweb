using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enerweb.Data.Migrations
{
    /// <inheritdoc />
    public partial class UniqueReadingTimestampPerMeter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Readings_MeterId_Timestamp",
                table: "Readings");

            migrationBuilder.CreateIndex(
                name: "IX_Readings_MeterId_Timestamp",
                table: "Readings",
                columns: new[] { "MeterId", "Timestamp" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Readings_MeterId_Timestamp",
                table: "Readings");

            migrationBuilder.CreateIndex(
                name: "IX_Readings_MeterId_Timestamp",
                table: "Readings",
                columns: new[] { "MeterId", "Timestamp" });
        }
    }
}
