using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Voltaguard.BLL.Security;
using Voltaguard.Common.Constants;
using Voltaguard.Common.Dtos;
using Voltaguard.Common.Requests;
using Voltaguard.DAL.Entities;

namespace Voltaguard.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<UserAccount> _userManager;
    private readonly IJwtTokenService _tokenService;

    public AuthController(UserManager<UserAccount> userManager, IJwtTokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    /// <summary>Creates a new customer account and returns a signed-in session.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CurrentUserDto>> Register([FromBody] RegisterRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            return BadRequest("An account with this email already exists.");
        }

        var user = new UserAccount
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            Role = RoleConstants.Customer
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        await _userManager.AddToRoleAsync(user, RoleConstants.Customer);
        return Ok(BuildSession(user));
    }

    /// <summary>Validates credentials and returns a JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserDto>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized();
        }

        return Ok(BuildSession(user));
    }

    /// <summary>Returns the current user, used by the frontend to restore a session.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserDto>> Me()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(BuildSession(user));
    }

    private CurrentUserDto BuildSession(UserAccount user) => new()
    {
        UserId = user.Id,
        Email = user.Email ?? string.Empty,
        FullName = user.FullName,
        Role = user.Role,
        Token = _tokenService.CreateToken(user)
    };
}
