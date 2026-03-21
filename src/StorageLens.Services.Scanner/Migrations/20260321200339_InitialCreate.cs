using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorageLens.Services.Scanner.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "scanner");

            migrationBuilder.CreateTable(
                name: "ScanError",
                schema: "scanner",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScanExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ErrorType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanError", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScanExecution",
                schema: "scanner",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScanJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RootPath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FilesDiscovered = table.Column<long>(type: "bigint", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanExecution", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScanError_ScanExecutionId",
                schema: "scanner",
                table: "ScanError",
                column: "ScanExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScanExecution_CorrelationId",
                schema: "scanner",
                table: "ScanExecution",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ScanExecution_ScanJobId",
                schema: "scanner",
                table: "ScanExecution",
                column: "ScanJobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScanError",
                schema: "scanner");

            migrationBuilder.DropTable(
                name: "ScanExecution",
                schema: "scanner");
        }
    }
}
