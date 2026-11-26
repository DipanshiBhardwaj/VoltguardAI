using Voltaguard.DAL.Entities;

namespace Voltaguard.BLL.Security;

public interface IJwtTokenService
{
    /// <summary>Issues a signed JWT carrying the user's id, email and role claim.</summary>
    string CreateToken(UserAccount user);
}
