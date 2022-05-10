using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace missinglink.Migrations
{
    public partial class AddingStatistics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusStops");

            migrationBuilder.CreateTable(
                name: "BusStatistics",
                columns: table => new
                {
                    BatchId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DelayedBuses = table.Column<int>(type: "integer", nullable: false),
                    TotalBuses = table.Column<int>(type: "integer", nullable: false),
                    CancelledBuses = table.Column<int>(type: "integer", nullable: false),
                    EarlyBuses = table.Column<int>(type: "integer", nullable: false),
                    OnTimeBuses = table.Column<int>(type: "integer", nullable: false),
                    NotReportingTimeBuses = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusStatistics", x => x.BatchId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusStatistics");

            migrationBuilder.CreateTable(
                name: "BusStops",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StopCode = table.Column<string>(type: "text", nullable: true),
                    StopDescription = table.Column<string>(type: "text", nullable: true),
                    StopId = table.Column<string>(type: "text", nullable: true),
                    StopLat = table.Column<decimal>(type: "numeric", nullable: false),
                    StopLon = table.Column<decimal>(type: "numeric", nullable: false),
                    StopName = table.Column<string>(type: "text", nullable: true),
                    ZoneId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusStops", x => x.Id);
                });
        }
    }
}
