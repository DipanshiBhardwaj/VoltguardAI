using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltaguard.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "AspNetUsers",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TelemetryLedger_FrequencyHz",
                table: "TelemetryLedger",
                sql: "\"FrequencyHz\" BETWEEN 47 AND 53");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TelemetryLedger_PowerFactor",
                table: "TelemetryLedger",
                sql: "\"PowerFactor\" BETWEEN 0 AND 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TelemetryLedger_VoltageV",
                table: "TelemetryLedger",
                sql: "\"VoltageV\" BETWEEN 100 AND 300");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SmartMeterAsset_SlabCode",
                table: "SmartMeterAsset",
                sql: "\"SlabCode\" IN ('DomesticLifeline', 'DomesticStandard')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EnergyAuditReport_Status",
                table: "EnergyAuditReport",
                sql: "\"Status\" IN ('Pending', 'Completed')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AspNetUsers_Role",
                table: "AspNetUsers",
                sql: "\"Role\" IN ('Customer', 'Admin')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TelemetryLedger_FrequencyHz",
                table: "TelemetryLedger");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TelemetryLedger_PowerFactor",
                table: "TelemetryLedger");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TelemetryLedger_VoltageV",
                table: "TelemetryLedger");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SmartMeterAsset_SlabCode",
                table: "SmartMeterAsset");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EnergyAuditReport_Status",
                table: "EnergyAuditReport");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AspNetUsers_Role",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);
        }
    }
}
