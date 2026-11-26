using Voltaguard.Common.Enums;
using Voltaguard.DAL.Entities;

namespace Voltaguard.DAL.Repositories;

public interface ITariffRepository
{
    Task<List<TariffSlab>> GetBySlabCodeAsync(SlabCode slabCode, CancellationToken cancellationToken = default);
}
