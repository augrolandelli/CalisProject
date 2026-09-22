namespace CalisApi.Models;

/// <summary>Logro (achievement) del catálogo. Lo otorga un profesor o admin.</summary>
public class Achievement
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Icon { get; set; }
    /// <summary>Oculto del catálogo (borrado lógico cuando ya fue otorgado). El historial se conserva.</summary>
    public bool IsHidden { get; set; }

    public ICollection<SessionAchievement> SessionAchievements { get; set; } = [];
    public ICollection<UserAchievement> UserAchievements { get; set; } = [];
}

/// <summary>Logro asociado a una clase (los que se pueden ganar en ella).</summary>
public class SessionAchievement
{
    public int Id { get; set; }

    public int SessionId { get; set; }
    public Session Session { get; set; } = null!;

    public int AchievementId { get; set; }
    public Achievement Achievement { get; set; } = null!;
}

/// <summary>Logro obtenido por un usuario, con fecha y clase de origen (si aplica).</summary>
public class UserAchievement
{
    public int Id { get; set; }
    public DateTime DateEarned { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int AchievementId { get; set; }
    public Achievement Achievement { get; set; } = null!;

    public int? SessionId { get; set; }
    public Session? Session { get; set; }
}
