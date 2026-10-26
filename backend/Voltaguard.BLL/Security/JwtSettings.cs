namespace Voltaguard.BLL.Security;

/// <summary>JWT signing and lifetime settings, bound from configuration.</summary>
public sealed class JwtSettings
{
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
    public int ExpiryMinutes { get; init; } = 120;
}
