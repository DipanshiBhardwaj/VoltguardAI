using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voltaguard.Common.Enums;
using Voltaguard.DAL;
using Voltaguard.DAL.Entities;
using Voltaguard.DAL.Repositories;
using Xunit;

namespace Voltaguard.DAL.Tests;

public class MeterRepositoryTests
{
    private static VoltaguardDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<VoltaguardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VoltaguardDbContext(options);
    }

    [Fact]
    public async Task GetByOwnerAsync_ReturnsOnlyThatOwnersMeters()
    {
        await using var db = NewContext();
        db.SmartMeterAssets.AddRange(
            new SmartMeterAsset { SerialNumber = "A-1", SlabCode = SlabCode.DomesticStandard, OwnerUserId = 1 },
            new SmartMeterAsset { SerialNumber = "A-2", SlabCode = SlabCode.DomesticStandard, OwnerUserId = 1 },
            new SmartMeterAsset { SerialNumber = "B-1", SlabCode = SlabCode.DomesticStandard, OwnerUserId = 2 });
        await db.SaveChangesAsync();

        var repo = new MeterRepository(db);
        var owner1 = await repo.GetByOwnerAsync(1);

        owner1.Should().HaveCount(2);
        owner1.Should().OnlyContain(m => m.OwnerUserId == 1);
    }

    [Fact]
    public async Task GetOwnedByIdAsync_OtherUsersMeter_ReturnsNull()
    {
        await using var db = NewContext();
        var meter = new SmartMeterAsset { SerialNumber = "A-1", SlabCode = SlabCode.DomesticStandard, OwnerUserId = 1 };
        db.SmartMeterAssets.Add(meter);
        await db.SaveChangesAsync();

        var repo = new MeterRepository(db);
        var result = await repo.GetOwnedByIdAsync(meter.Id, ownerUserId: 2);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SerialExistsForOtherMeterAsync_OnlyTheMeterItselfHasSerial_ReturnsFalse()
    {
        await using var db = NewContext();
        var meter = new SmartMeterAsset { SerialNumber = "A-1", SlabCode = SlabCode.DomesticStandard, OwnerUserId = 1 };
        db.SmartMeterAssets.Add(meter);
        await db.SaveChangesAsync();

        var result = await new MeterRepository(db).SerialExistsForOtherMeterAsync("A-1", meter.Id);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SerialExistsForOtherMeterAsync_AnotherMeterHasSerial_ReturnsTrue()
    {
        await using var db = NewContext();
        var meter = new SmartMeterAsset { SerialNumber = "A-1", SlabCode = SlabCode.DomesticStandard, OwnerUserId = 1 };
        db.SmartMeterAssets.AddRange(meter, new SmartMeterAsset { SerialNumber = "B-1", SlabCode = SlabCode.DomesticStandard, OwnerUserId = 2 });
        await db.SaveChangesAsync();

        var result = await new MeterRepository(db).SerialExistsForOtherMeterAsync("B-1", meter.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Remove_ThenSave_SoftDeletesMeter()
    {
        await using var db = NewContext();
        var meter = new SmartMeterAsset { SerialNumber = "A-1", SlabCode = SlabCode.DomesticStandard, OwnerUserId = 1 };
        db.SmartMeterAssets.Add(meter);
        await db.SaveChangesAsync();

        var repo = new MeterRepository(db);
        repo.Remove(meter);
        await repo.SaveChangesAsync();

        (await repo.GetOwnedByIdAsync(meter.Id, 1)).Should().BeNull();
        (await db.SmartMeterAssets.IgnoreQueryFilters().SingleAsync(m => m.Id == meter.Id)).IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetByOwnerAsync_ExcludesSoftDeletedMeters()
    {
        await using var db = NewContext();
        var live = new SmartMeterAsset { SerialNumber = "A-1", SlabCode = SlabCode.DomesticStandard, OwnerUserId = 1 };
        var removed = new SmartMeterAsset { SerialNumber = "A-2", SlabCode = SlabCode.DomesticStandard, OwnerUserId = 1 };
        db.SmartMeterAssets.AddRange(live, removed);
        await db.SaveChangesAsync();

        db.SmartMeterAssets.Remove(removed); // soft delete via SaveChanges override
        await db.SaveChangesAsync();

        var repo = new MeterRepository(db);
        var result = await repo.GetByOwnerAsync(1);

        result.Should().ContainSingle();
        result[0].SerialNumber.Should().Be("A-1");
    }
}
