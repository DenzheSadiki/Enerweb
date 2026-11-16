using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enerweb.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddErrorLogging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Errors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExceptionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InnerException = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Errors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DatabaseLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ErrorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TraceId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RequestPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatabaseLogs_Errors_ErrorId",
                        column: x => x.ErrorId,
                        principalTable: "Errors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseLogs_ErrorId",
                table: "DatabaseLogs",
                column: "ErrorId");

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseLogs_Timestamp",
                table: "DatabaseLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseLogs_TraceId",
                table: "DatabaseLogs",
                column: "TraceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DatabaseLogs");

            migrationBuilder.DropTable(
                name: "Errors");
        }
    }
}
