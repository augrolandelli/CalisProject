namespace CalisApi.Models;

/// <summary>
/// Suscripción Web Push de un dispositivo del usuario (VAPID).
/// Un usuario puede tener varias (teléfono, desktop, etc.).
/// </summary>
public class PushSubscription
{
    public int Id { get; set; }
    public required string Endpoint { get; set; }
    public required string P256dh { get; set; }
    public required string Auth { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
