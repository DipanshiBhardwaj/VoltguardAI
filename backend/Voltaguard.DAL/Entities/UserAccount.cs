using Microsoft.AspNetCore.Identity;
using Voltaguard.Common.Constants;

namespace Voltaguard.DAL.Entities;

/// <summary>
/// An application user. Extends IdentityUser so password hashing and lockout are
/// handled by ASP.NET Identity; the int key keeps foreign keys small and readable.
/// </summary>
public class UserAccount : IdentityUser<int>
{
    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = RoleConstants.Customer;

    public ICollection<SmartMeterAsset> Meters { get; set; } = new List<SmartMeterAsset>();
}
