using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorageLens.Services.Locations.Migrations;

public partial class AddLocationMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SiteLocation",
            schema: "locations",
            table: "StorageLocations",
            type: "nvarchar(160)",
            maxLength: 160,
            nullable: false,
            defaultValue: "Unspecified");

        migrationBuilder.AddColumn<double>(
            name: "Latitude",
            schema: "locations",
            table: "StorageLocations",
            type: "float",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "Longitude",
            schema: "locations",
            table: "StorageLocations",
            type: "float",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Latitude",
            schema: "locations",
            table: "StorageLocations");

        migrationBuilder.DropColumn(
            name: "Longitude",
            schema: "locations",
            table: "StorageLocations");

        migrationBuilder.DropColumn(
            name: "SiteLocation",
            schema: "locations",
            table: "StorageLocations");
    }
}
