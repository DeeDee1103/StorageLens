using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorageLens.Services.Locations.Migrations;

public partial class AddLocationVisibilitySoftDelete : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsVisibleInUI",
            schema: "locations",
            table: "StorageLocations",
            type: "bit",
            nullable: false,
            defaultValue: true);

        migrationBuilder.CreateIndex(
            name: "IX_StorageLocations_IsVisibleInUI",
            schema: "locations",
            table: "StorageLocations",
            column: "IsVisibleInUI");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_StorageLocations_IsVisibleInUI",
            schema: "locations",
            table: "StorageLocations");

        migrationBuilder.DropColumn(
            name: "IsVisibleInUI",
            schema: "locations",
            table: "StorageLocations");
    }
}
