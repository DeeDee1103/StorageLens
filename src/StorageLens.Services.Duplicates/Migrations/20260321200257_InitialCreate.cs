using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorageLens.Services.Duplicates.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "duplicates");

            migrationBuilder.CreateTable(
                name: "DuplicateGroups",
                schema: "duplicates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScanJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    HashValue = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FileCount = table.Column<int>(type: "int", nullable: false),
                    TotalSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    WastedBytes = table.Column<long>(type: "bigint", nullable: false),
                    ExampleFileName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    LocationsCsv = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RecalculatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuplicateGroups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DuplicateGroups_CorrelationId",
                schema: "duplicates",
                table: "DuplicateGroups",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_DuplicateGroups_RecalculatedAtUtc",
                schema: "duplicates",
                table: "DuplicateGroups",
                column: "RecalculatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DuplicateGroups_ScanJobId_HashValue",
                schema: "duplicates",
                table: "DuplicateGroups",
                columns: new[] { "ScanJobId", "HashValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DuplicateGroups_WastedBytes",
                schema: "duplicates",
                table: "DuplicateGroups",
                column: "WastedBytes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DuplicateGroups",
                schema: "duplicates");
        }
    }
}
