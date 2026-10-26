using Voltaguard.Common.Dtos;
using Voltaguard.Common.Requests;
using Voltaguard.DAL.Entities;
using Voltaguard.DAL.Repositories;

namespace Voltaguard.BLL.Services;

public class MeterService : IMeterService
{
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
            var recent = await _telemetryRepository.GetRecentByMeterAsync(meter.Id, 1, cancellationToken);
            result.Add(new MeterDto
            {
                MeterAssetId = meter.Id,
                SerialNumber = meter.SerialNumber,
                SlabCode = meter.SlabCode,
                ReadingCount = meter.Readings.Count,
                LatestReadingOn = recent.Count > 0 ? recent[0].IntervalEnding : null
            });
        }

        return result;
    }

    public async Task<MeterDto> CreateAsync(int ownerUserId, CreateMeterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await _meterRepository.SerialExistsAsync(request.SerialNumber, cancellationToken))
        {
            throw new InvalidOperationException("A meter with this serial number already exists.");
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
            LatestReadingOn = null
        };
    }

    public async Task<List<TelemetryRowDto>> GetRecentReadingsAsync(
        int meterAssetId,
        int ownerUserId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var meter = await _meterRepository.GetOwnedByIdAsync(meterAssetId, ownerUserId, cancellationToken);
        if (meter is null)
        {
            throw new InvalidOperationException("Meter not found for the current user.");
        }

        var readings = await _telemetryRepository.GetRecentByMeterAsync(meterAssetId, take, cancellationToken);

        return readings.Select(t => new TelemetryRowDto
        {
            LogId = t.LogId,
            IntervalEnding = t.IntervalEnding,
            VoltageV = t.VoltageV,
            ActivePowerKW = t.ActivePowerKW,
            PowerFactor = t.PowerFactor,
            FrequencyHz = t.FrequencyHz,
            CumulativeKWH = t.CumulativeKWH
        }).ToList();
    }
}
