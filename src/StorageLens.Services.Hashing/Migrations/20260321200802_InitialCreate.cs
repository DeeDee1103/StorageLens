using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorageLens.Services.Hashing.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "hashing");

            migrationBuilder.CreateTable(
                name: "HashExecution",
                schema: "hashing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScanJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Attempted = table.Column<int>(type: "int", nullable: false),
                    Succeeded = table.Column<int>(type: "int", nullable: false),
                    Failed = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HashExecution", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HashFailure",
                schema: "hashing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HashExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HashFailure", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HashExecution_CorrelationId",
                schema: "hashing",
                table: "HashExecution",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_HashExecution_ScanJobId",
                schema: "hashing",
                table: "HashExecution",
                column: "ScanJobId");

            migrationBuilder.CreateIndex(
                name: "IX_HashFailure_HashExecutionId",
                schema: "hashing",
                table: "HashFailure",
                column: "HashExecutionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HashExecution",
                schema: "hashing");

            migrationBuilder.DropTable(
                name: "HashFailure",
                schema: "hashing");
        }
    }
}
