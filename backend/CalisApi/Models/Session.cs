namespace CalisApi.Models;

/// <summary>
/// Clase en vivo (sesión) con cupos limitados.
/// El contador <see cref="Enrolled"/> se gestiona con transacciones (anti-overbooking).
/// </summary>
public class Session
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public DateTime Date { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public int LimitedSpots { get; set; }
    public int Enrolled { get; set; }
    public required string Difficulty { get; set; }
    public required string CoachName { get; set; }

    /// <summary>Fecha en que se notificó por push a los admins que la clase terminó.</summary>
    public DateTime? AdminNotifiedAt { get; set; }

    public ICollection<UserSession> Enrollments { get; set; } = [];
    public ICollection<SessionWaitlist> Waitlist { get; set; } = [];
}

/// <summary>Inscripción de un usuario a una clase.</summary>
public class UserSession
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Null = no marcado aún, true = asistió, false = no-show.</summary>
    public bool? Attended { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int SessionId { get; set; }
    public Session Session { get; set; } = null!;
}

/// <summary>Entrada en la lista de espera de una clase llena.</summary>
public class SessionWaitlist
{
    public int Id { get; set; }
    public int Position { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int SessionId { get; set; }
    public Session Session { get; set; } = null!;
}
