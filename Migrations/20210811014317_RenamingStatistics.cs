using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace missinglink.Migrations
{
    public partial class RenamingStatistics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusStatistics");

            migrationBuilder.CreateTable(
                name: "BusStatistic",
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
                    table.PrimaryKey("PK_BusStatistic", x => x.BatchId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusStatistic");

            migrationBuilder.CreateTable(
                name: "BusStatistics",
                columns: table => new
                {
                    BatchId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CancelledBuses = table.Column<int>(type: "integer", nullable: false),
                    DelayedBuses = table.Column<int>(type: "integer", nullable: false),
                    EarlyBuses = table.Column<int>(type: "integer", nullable: false),
                    NotReportingTimeBuses = table.Column<int>(type: "integer", nullable: false),
                    OnTimeBuses = table.Column<int>(type: "integer", nullable: false),
                    TotalBuses = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusStatistics", x => x.BatchId);
                });
        }
    }
}
