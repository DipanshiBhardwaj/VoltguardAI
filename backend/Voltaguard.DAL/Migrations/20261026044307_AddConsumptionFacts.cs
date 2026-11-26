using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltaguard.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddConsumptionFacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AverageDailyKwh",
                table: "EnergyAuditReport",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DayKwh",
                table: "EnergyAuditReport",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EveningKwh",
                table: "EnergyAuditReport",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HighestDemandKW",
                table: "EnergyAuditReport",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HighestDemandOn",
                table: "EnergyAuditReport",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MorningKwh",
                table: "EnergyAuditReport",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NightKwh",
                table: "EnergyAuditReport",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PeakHour",
                table: "EnergyAuditReport",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageDailyKwh",
                table: "EnergyAuditReport");

            migrationBuilder.DropColumn(
                name: "DayKwh",
                table: "EnergyAuditReport");

            migrationBuilder.DropColumn(
                name: "EveningKwh",
                table: "EnergyAuditReport");

            migrationBuilder.DropColumn(
                name: "HighestDemandKW",
                table: "EnergyAuditReport");

            migrationBuilder.DropColumn(
                name: "HighestDemandOn",
                table: "EnergyAuditReport");

            migrationBuilder.DropColumn(
                name: "MorningKwh",
                table: "EnergyAuditReport");

            migrationBuilder.DropColumn(
                name: "NightKwh",
                table: "EnergyAuditReport");

            migrationBuilder.DropColumn(
                name: "PeakHour",
                table: "EnergyAuditReport");
        }
    }
}
