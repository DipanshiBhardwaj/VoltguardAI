using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltaguard.DAL.Migrations
{
    /// <inheritdoc />
    public partial class UniqueReadingPerInterval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TelemetryLedger_MeterAssetId_IntervalEnding",
                table: "TelemetryLedger");

            // Files uploaded twice before this fix left duplicate rows behind, which
            // would make the unique index fail. Keep the first copy of each interval.
            migrationBuilder.Sql(
                """
                DELETE FROM "TelemetryLedger" dup
                USING "TelemetryLedger" keep
                WHERE dup."MeterAssetId" = keep."MeterAssetId"
                  AND dup."IntervalEnding" = keep."IntervalEnding"
                  AND dup."LogId" > keep."LogId";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryLedger_MeterAssetId_IntervalEnding",
                table: "TelemetryLedger",
                columns: new[] { "MeterAssetId", "IntervalEnding" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TelemetryLedger_MeterAssetId_IntervalEnding",
                table: "TelemetryLedger");

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryLedger_MeterAssetId_IntervalEnding",
                table: "TelemetryLedger",
                columns: new[] { "MeterAssetId", "IntervalEnding" });
        }
    }
}
