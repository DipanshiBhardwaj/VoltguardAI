namespace Voltaguard.DAL.Entities;

/// <summary>
/// One 15-minute meter reading. Append-only and high-volume, so it uses a bigint
/// key and is not soft-deletable. Rows are written by the file ingestion parser,
/// one per valid line in the uploaded ledger.
/// </summary>
public class TelemetryLedger
{
    public long LogId { get; set; }

    public int MeterAssetId { get; set; }
    public SmartMeterAsset? Meter { get; set; }

    public DateTimeOffset IntervalEnding { get; set; }

    public decimal VoltageV { get; set; }
    public decimal ActivePowerKW { get; set; }
    public decimal PhaseCurrentA { get; set; }
    public decimal NeutralCurrentA { get; set; }
    public decimal PowerFactor { get; set; }
    public decimal FrequencyHz { get; set; }
    public decimal CumulativeKWH { get; set; }

    public bool TamperFlag { get; set; }
}
