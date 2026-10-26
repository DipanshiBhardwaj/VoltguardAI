using System.Security.Claims;

namespace Voltaguard.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Reads the signed-in user's id from the JWT, as the server trusts it.</summary>
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(value, out var userId))
        {
            throw new InvalidOperationException("The authenticated user id is missing from the token.");
        }

        return userId;
    }
}
