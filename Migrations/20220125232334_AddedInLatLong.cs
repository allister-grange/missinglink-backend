using Microsoft.EntityFrameworkCore.Migrations;

namespace missinglink.Migrations
{
    public partial class AddedInLatLong : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Bearing",
                table: "Buses",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Lat",
                table: "Buses",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Long",
                table: "Buses",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bearing",
                table: "Buses");

            migrationBuilder.DropColumn(
                name: "Lat",
                table: "Buses");

            migrationBuilder.DropColumn(
                name: "Long",
                table: "Buses");
        }
    }
}
