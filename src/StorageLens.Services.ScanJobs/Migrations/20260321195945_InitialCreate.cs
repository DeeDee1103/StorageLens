using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorageLens.Services.ScanJobs.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "scanjobs");

            migrationBuilder.CreateTable(
                name: "ScanJobs",
                schema: "scanjobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CurrentStage = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FilesScanned = table.Column<long>(type: "bigint", nullable: false),
                    FilesHashed = table.Column<long>(type: "bigint", nullable: false),
                    DuplicateGroupsFound = table.Column<int>(type: "int", nullable: false),
                    WarningCount = table.Column<int>(type: "int", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    ProgressPercent = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanJobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScanJobs_CorrelationId",
                schema: "scanjobs",
                table: "ScanJobs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ScanJobs_StartTime",
                schema: "scanjobs",
                table: "ScanJobs",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_ScanJobs_Status",
                schema: "scanjobs",
                table: "ScanJobs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScanJobs",
                schema: "scanjobs");
        }
    }
}
