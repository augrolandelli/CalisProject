namespace CalisApi.Auth;

/// <summary>
/// Configuración fuertemente tipada de JWT (sección "Jwt" de appsettings).
/// La Key nunca se commitea: user-secrets en dev, variables de entorno en prod.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public required string Key { get; set; }
    public required string Issuer { get; set; }
    public required string Audience { get; set; }
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 14;
}
