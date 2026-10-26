using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Voltaguard.DAL;

/// <summary>
/// Used only by the EF Core command-line tools (migrations). The running app
/// builds the context from configuration instead; this just points the tooling
/// at the local development database.
/// </summary>
public class VoltaguardDbContextFactory : IDesignTimeDbContextFactory<VoltaguardDbContext>
{
    public VoltaguardDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<VoltaguardDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=voltaguard;Username=voltaguard;Password=voltaguard_dev_pw")
            .Options;

        return new VoltaguardDbContext(options);
    }
}
