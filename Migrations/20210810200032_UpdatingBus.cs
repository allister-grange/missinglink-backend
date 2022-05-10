using Microsoft.EntityFrameworkCore.Migrations;

namespace missinglink.Migrations
{
    public partial class UpdatingBus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RouteDescription",
                table: "Buses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouteId",
                table: "Buses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouteLongName",
                table: "Buses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouteShortName",
                table: "Buses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TripId",
                table: "Buses",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RouteDescription",
                table: "Buses");

            migrationBuilder.DropColumn(
                name: "RouteId",
                table: "Buses");

            migrationBuilder.DropColumn(
                name: "RouteLongName",
                table: "Buses");

            migrationBuilder.DropColumn(
                name: "RouteShortName",
                table: "Buses");

            migrationBuilder.DropColumn(
                name: "TripId",
                table: "Buses");
        }
    }
}
