namespace Voltaguard.Common.Constants;

/// <summary>
/// Role names used for JWT claims and authorization policies. Kept in one place
/// so the token, the policy registration and the seeder never drift apart.
/// </summary>
public static class RoleConstants
{
    public const string Customer = "Customer";
    public const string Admin = "Admin";
}
