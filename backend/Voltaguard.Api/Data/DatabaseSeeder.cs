using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Voltaguard.Common.Constants;
using Voltaguard.Common.Enums;
using Voltaguard.DAL;
using Voltaguard.DAL.Entities;

namespace Voltaguard.Api.Data;

/// <summary>
/// Seeds the baseline data the app needs to run: the two roles, an admin and a
/// demo customer, the HPSEBL-style domestic tariff slabs, and a demo meter to
/// upload readings against.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = provider.GetRequiredService<UserManager<UserAccount>>();
        var db = provider.GetRequiredService<VoltaguardDbContext>();

        await SeedRoles(roleManager);
        var customer = await SeedUsers(userManager);
        await SeedTariffSlabs(db);
        await SeedDemoMeter(db, customer.Id);
    }

    private static async Task SeedRoles(RoleManager<IdentityRole<int>> roleManager)
    {
        foreach (var role in new[] { RoleConstants.Admin, RoleConstants.Customer })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(role));
            }
        }
    }

    private static async Task<UserAccount> SeedUsers(UserManager<UserAccount> userManager)
    {
        await EnsureUser(userManager, "admin@voltaguard.local", "Admin@123", "System Administrator", RoleConstants.Admin);
        return await EnsureUser(userManager, "customer@voltaguard.local", "Customer@123", "Demo Customer", RoleConstants.Customer);
    }

    private static async Task<UserAccount> EnsureUser(
        UserManager<UserAccount> userManager, string email, string password, string fullName, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            return user;
        }

        user = new UserAccount
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            Role = role,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, role);
        return user;
    }

    private static async Task SeedTariffSlabs(VoltaguardDbContext db)
    {
        if (await db.TariffSlabs.IgnoreQueryFilters().AnyAsync())
        {
            return;
        }

        // HPSEBL-style domestic slabs. The standard category also carries a small
        // Time-of-Day premium on its top slab to show the ToD multiplier in action.
        db.TariffSlabs.AddRange(
            new TariffSlab { SlabCode = SlabCode.DomesticLifeline, FromUnit = 0, ToUnit = 60, RatePerUnit = 3.00m, ToDMultiplier = 1.0m, FixedCharge = 30 },

            new TariffSlab { SlabCode = SlabCode.DomesticStandard, FromUnit = 0, ToUnit = 125, RatePerUnit = 4.00m, ToDMultiplier = 1.0m, FixedCharge = 70 },
            new TariffSlab { SlabCode = SlabCode.DomesticStandard, FromUnit = 125, ToUnit = 300, RatePerUnit = 5.25m, ToDMultiplier = 1.0m, FixedCharge = 0 },
            new TariffSlab { SlabCode = SlabCode.DomesticStandard, FromUnit = 300, ToUnit = null, RatePerUnit = 6.00m, ToDMultiplier = 1.15m, FixedCharge = 0 });

        await db.SaveChangesAsync();
    }

    private static async Task SeedDemoMeter(VoltaguardDbContext db, int ownerUserId)
    {
        if (await db.SmartMeterAssets.AnyAsync(m => m.OwnerUserId == ownerUserId))
        {
            return;
        }

        db.SmartMeterAssets.Add(new SmartMeterAsset
        {
            SerialNumber = "HPSEBL-DEMO-0001",
            SlabCode = SlabCode.DomesticStandard,
            OwnerUserId = ownerUserId
        });

        await db.SaveChangesAsync();
    }
}
