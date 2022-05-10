﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using missinglink.Contexts;

namespace missinglink.Migrations
{
    [DbContext(typeof(BusContext))]
    [Migration("20220125232334_AddedInLatLong")]
    partial class AddedInLatLong
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.8")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            modelBuilder.Entity("missinglink.Models.Bus", b =>
                {
                    b.Property<string>("VehicleId")
                        .HasColumnType("text");

                    b.Property<decimal>("Bearing")
                        .HasColumnType("numeric");

                    b.Property<int>("Delay")
                        .HasColumnType("integer");

                    b.Property<decimal>("Lat")
                        .HasColumnType("numeric");

                    b.Property<decimal>("Long")
                        .HasColumnType("numeric");

                    b.Property<string>("RouteDescription")
                        .HasColumnType("text");

                    b.Property<string>("RouteId")
                        .HasColumnType("text");

                    b.Property<string>("RouteLongName")
                        .HasColumnType("text");

                    b.Property<string>("RouteShortName")
                        .HasColumnType("text");

                    b.Property<string>("Status")
                        .HasColumnType("text");

                    b.Property<string>("StopId")
                        .HasColumnType("text");

                    b.Property<string>("TripId")
                        .HasColumnType("text");

                    b.HasKey("VehicleId");

                    b.ToTable("Buses");
                });

            modelBuilder.Entity("missinglink.Models.BusStatistic", b =>
                {
                    b.Property<int>("BatchId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int>("CancelledBuses")
                        .HasColumnType("integer");

                    b.Property<int>("DelayedBuses")
                        .HasColumnType("integer");

                    b.Property<int>("EarlyBuses")
                        .HasColumnType("integer");

                    b.Property<int>("NotReportingTimeBuses")
                        .HasColumnType("integer");

                    b.Property<int>("OnTimeBuses")
                        .HasColumnType("integer");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int>("TotalBuses")
                        .HasColumnType("integer");

                    b.HasKey("BatchId");

                    b.ToTable("BusStatistic");
                });
#pragma warning restore 612, 618
        }
    }
}
