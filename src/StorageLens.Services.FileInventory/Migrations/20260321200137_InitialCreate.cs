using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorageLens.Services.FileInventory.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "fileinventory");

            migrationBuilder.CreateTable(
                name: "FileRecords",
                schema: "fileinventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScanJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Extension = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastAccessedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DiscoveryDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ScanSourcePath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    LastModifiedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    HashValue = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    HashStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HashCalculatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    HashError = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: true),
                    IsDuplicate = table.Column<bool>(type: "bit", nullable: false),
                    DuplicateGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileRecords_CorrelationId",
                schema: "fileinventory",
                table: "FileRecords",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_FileRecords_Extension",
                schema: "fileinventory",
                table: "FileRecords",
                column: "Extension");

            migrationBuilder.CreateIndex(
                name: "IX_FileRecords_FileName",
                schema: "fileinventory",
                table: "FileRecords",
                column: "FileName");

            migrationBuilder.CreateIndex(
                name: "IX_FileRecords_FilePath",
                schema: "fileinventory",
                table: "FileRecords",
                column: "FilePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileRecords_HashStatus",
                schema: "fileinventory",
                table: "FileRecords",
                column: "HashStatus");

            migrationBuilder.CreateIndex(
                name: "IX_FileRecords_HashValue",
                schema: "fileinventory",
                table: "FileRecords",
                column: "HashValue");

            migrationBuilder.CreateIndex(
                name: "IX_FileRecords_LastModifiedDate",
                schema: "fileinventory",
                table: "FileRecords",
                column: "LastModifiedDate");

            migrationBuilder.CreateIndex(
                name: "IX_FileRecords_ScanJobId",
                schema: "fileinventory",
                table: "FileRecords",
                column: "ScanJobId");

            migrationBuilder.CreateIndex(
                name: "IX_FileRecords_SizeBytes",
                schema: "fileinventory",
                table: "FileRecords",
                column: "SizeBytes");

            migrationBuilder.CreateIndex(
                name: "IX_FileRecords_StorageLocationId",
                schema: "fileinventory",
                table: "FileRecords",
                column: "StorageLocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileRecords",
                schema: "fileinventory");
        }
    }
}
