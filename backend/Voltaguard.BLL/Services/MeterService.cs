using Voltaguard.Common.Dtos;
using Voltaguard.Common.Requests;
using Voltaguard.DAL.Entities;
using Voltaguard.DAL.Repositories;

namespace Voltaguard.BLL.Services;

public class MeterService : IMeterService
{
    private const string MeterNotFound = "Meter not found for the current user.";
    private const string DuplicateSerial = "A meter with this serial number already exists.";

    private readonly IMeterRepository _meterRepository;
    private readonly ITelemetryRepository _telemetryRepository;

    public MeterService(IMeterRepository meterRepository, ITelemetryRepository telemetryRepository)
    {
        _meterRepository = meterRepository;
        _telemetryRepository = telemetryRepository;
    }

    public async Task<List<MeterDto>> GetMyMetersAsync(int ownerUserId, CancellationToken cancellationToken = default)
    {
        var meters = await _meterRepository.GetByOwnerAsync(ownerUserId, cancellationToken);
        var result = new List<MeterDto>();

        foreach (var meter in meters)
        {
            result.Add(await ToDtoAsync(meter, cancellationToken));
        }

        return result;
    }

    public async Task<MeterDto> CreateAsync(int ownerUserId, CreateMeterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await _meterRepository.SerialExistsAsync(request.SerialNumber, cancellationToken))
        {
            throw new InvalidOperationException(DuplicateSerial);
        }

        var meter = new SmartMeterAsset
        {
            SerialNumber = request.SerialNumber,
            SlabCode = request.SlabCode,
            OwnerUserId = ownerUserId
        };

        await _meterRepository.AddAsync(meter, cancellationToken);
        await _meterRepository.SaveChangesAsync(cancellationToken);

        return new MeterDto
        {
            MeterAssetId = meter.Id,
            SerialNumber = meter.SerialNumber,
            SlabCode = meter.SlabCode,
            ReadingCount = 0,
            LatestReadingOn = null,
            AnomalyCount = 0
        };
    }

    public async Task<MeterDto> UpdateAsync(
        int meterAssetId,
        int ownerUserId,
        UpdateMeterRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var meter = await GetOwnedOrThrowAsync(meterAssetId, ownerUserId, cancellationToken);

        // The meter keeping its own serial is not a clash.
        if (await _meterRepository.SerialExistsForOtherMeterAsync(request.SerialNumber, meterAssetId, cancellationToken))
        {
            throw new InvalidOperationException(DuplicateSerial);
        }

        meter.SerialNumber = request.SerialNumber;
        meter.SlabCode = request.SlabCode;
        await _meterRepository.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(meter, cancellationToken);
    }

    public async Task DeleteAsync(int meterAssetId, int ownerUserId, CancellationToken cancellationToken = default)
    {
        var meter = await GetOwnedOrThrowAsync(meterAssetId, ownerUserId, cancellationToken);

        // Soft delete: the DbContext sets IsDeleted instead of removing the row, so
        // the readings and past audit reports stay in the database.
        _meterRepository.Remove(meter);
        await _meterRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<TelemetryRowDto>> GetRecentReadingsAsync(
        int meterAssetId,
        int ownerUserId,
        int take,
        CancellationToken cancellationToken = default)
    {
        await GetOwnedOrThrowAsync(meterAssetId, ownerUserId, cancellationToken);

        var readings = await _telemetryRepository.GetRecentByMeterAsync(meterAssetId, take, cancellationToken);
        return readings.Select(ToRowDto).ToList();
    }

    public async Task<List<TelemetryRowDto>> GetReadingsInRangeAsync(
        int meterAssetId,
        int ownerUserId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int take,
        CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtc) = ToUtcRange(from, to);
        await GetOwnedOrThrowAsync(meterAssetId, ownerUserId, cancellationToken);

        var readings = await _telemetryRepository.GetRangeByMeterAsync(meterAssetId, fromUtc, toUtc, take, cancellationToken);
        return readings.Select(ToRowDto).ToList();
    }

    public async Task<List<AnomalyReadingDto>> GetAnomaliesAsync(
        int meterAssetId,
        int ownerUserId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtc) = ToUtcRange(from, to);
        await GetOwnedOrThrowAsync(meterAssetId, ownerUserId, cancellationToken);

        // The repository returns them newest first; one entry per anomaly type.
        var candidates = await _telemetryRepository.GetAbnormalByMeterAsync(meterAssetId, fromUtc, toUtc, cancellationToken);
        return candidates
            .SelectMany(r => ReadingAnomalyDetector.Detect(r).Select(type => new AnomalyReadingDto
            {
                LogId = r.LogId,
                IntervalEnding = r.IntervalEnding,
                Type = type,
                VoltageV = r.VoltageV,
                PhaseCurrentA = r.PhaseCurrentA,
                NeutralCurrentA = r.NeutralCurrentA
            }))
            .ToList();
    }

    private async Task<SmartMeterAsset> GetOwnedOrThrowAsync(int meterAssetId, int ownerUserId, CancellationToken cancellationToken)
    {
        var meter = await _meterRepository.GetOwnedByIdAsync(meterAssetId, ownerUserId, cancellationToken);
        return meter ?? throw new InvalidOperationException(MeterNotFound);
    }

    private async Task<MeterDto> ToDtoAsync(SmartMeterAsset meter, CancellationToken cancellationToken)
    {
        var recent = await _telemetryRepository.GetRecentByMeterAsync(meter.Id, 1, cancellationToken);
        var readingCount = await _telemetryRepository.CountByMeterAsync(meter.Id, cancellationToken);
        var abnormal = await _telemetryRepository.GetAbnormalByMeterAsync(meter.Id, null, null, cancellationToken);

        return new MeterDto
        {
            MeterAssetId = meter.Id,
            SerialNumber = meter.SerialNumber,
            SlabCode = meter.SlabCode,
            ReadingCount = readingCount,
            LatestReadingOn = recent.Count > 0 ? recent[0].IntervalEnding : null,
            AnomalyCount = abnormal.Sum(r => ReadingAnomalyDetector.Detect(r).Count)
        };
    }

    // Clients send IST (or any offset); stored readings are UTC.
    private static (DateTimeOffset? FromUtc, DateTimeOffset? ToUtc) ToUtcRange(DateTimeOffset? from, DateTimeOffset? to)
    {
        if (from is { } f && to is { } t && t < f)
        {
            throw new InvalidOperationException("The end date cannot be before the start date.");
        }

        return (from?.ToUniversalTime(), to?.ToUniversalTime());
    }

    private static TelemetryRowDto ToRowDto(TelemetryLedger t) => new()
    {
        LogId = t.LogId,
        IntervalEnding = t.IntervalEnding,
        VoltageV = t.VoltageV,
        ActivePowerKW = t.ActivePowerKW,
        PowerFactor = t.PowerFactor,
        FrequencyHz = t.FrequencyHz,
        CumulativeKWH = t.CumulativeKWH
    };
}
