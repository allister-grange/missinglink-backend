using Microsoft.EntityFrameworkCore.Migrations;

namespace missinglink.Migrations
{
    public partial class RenamingStatistics2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "timestamp",
                table: "BusStatistic",
                newName: "Timestamp");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "BusStatistic",
                newName: "timestamp");
        }
    }
}
