using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorageLens.Services.Locations.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "locations");

            migrationBuilder.CreateTable(
                name: "StorageLocations",
                schema: "locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastScanDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TotalSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileCount = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageLocations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocations_Name",
                schema: "locations",
                table: "StorageLocations",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocations_TotalSizeBytes",
                schema: "locations",
                table: "StorageLocations",
                column: "TotalSizeBytes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StorageLocations",
                schema: "locations");
        }
    }
}
