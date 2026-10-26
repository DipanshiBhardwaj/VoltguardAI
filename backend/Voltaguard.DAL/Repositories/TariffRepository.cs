using Microsoft.EntityFrameworkCore;
using Voltaguard.Common.Enums;
using Voltaguard.DAL.Entities;

namespace Voltaguard.DAL.Repositories;

public class TariffRepository : ITariffRepository
{
    private readonly VoltaguardDbContext _db;

    public TariffRepository(VoltaguardDbContext db)
    {
        _db = db;
    }

    // Ordered from the lowest slab up, which is the order the billing engine walks them.
    public Task<List<TariffSlab>> GetBySlabCodeAsync(SlabCode slabCode, CancellationToken cancellationToken = default)
    {
        return _db.TariffSlabs
            .Where(s => s.SlabCode == slabCode)
            .OrderBy(s => s.FromUnit)
            .ToListAsync(cancellationToken);
    }
}
