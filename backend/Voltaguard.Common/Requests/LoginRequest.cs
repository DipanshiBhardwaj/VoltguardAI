using System.ComponentModel.DataAnnotations;

namespace Voltaguard.Common.Requests;

/// <summary>Credentials posted to the login endpoint.</summary>
public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}
