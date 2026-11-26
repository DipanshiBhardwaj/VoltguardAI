namespace Voltaguard.Common.Dtos;

/// <summary>Identity returned to the frontend after login and on session checks.</summary>
public sealed class CurrentUserDto
{
    public int UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
}
