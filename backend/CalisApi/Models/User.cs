namespace CalisApi.Models;

/// <summary>
/// Usuario de la plataforma. Roles: Guerrero (gratis), Clover (pago), Admin (staff).
/// </summary>
public class User
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public required string Phone { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string Role { get; set; } = Roles.Guerrero;
    public string State { get; set; } = "Activo";
    public string? PhotoUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}

/// <summary>Roles del sistema (modelo freemium — ver ESPECIFICACION_CALISAPP.md §2).</summary>
public static class Roles
{
    public const string Guerrero = "Guerrero";
    public const string Clover = "Clover";
    public const string Admin = "Admin";
}
