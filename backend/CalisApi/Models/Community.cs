namespace CalisApi.Models;

/// <summary>Tipos de publicaciones del tablón; los logros se generan desde el servidor.</summary>
public static class PostKinds
{
    public const string Announcement = "announcement";
    public const string Achievement = "achievement";
    public const string Competition = "competition";
}

/// <summary>Publicación exclusiva. El borrado lógico conserva la identidad de los logros automáticos.</summary>
public class Post
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public string Kind { get; set; } = PostKinds.Announcement;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public int? UserAchievementId { get; set; }
    public UserAchievement? UserAchievement { get; set; }
    public DateTime? StartsAt { get; set; }
    public string? Location { get; set; }
    public ICollection<PostLike> Likes { get; set; } = [];
    public ICollection<MediaAsset> Media { get; set; } = [];
}

/// <summary>Un único me gusta por miembro y publicación.</summary>
public class PostLike
{
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

/// <summary>Archivo privado: primero se sube a staging, después se valida y copia a una clave inmutable.</summary>
public class MediaAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int UploadedById { get; set; }
    public User UploadedBy { get; set; } = null!;
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long Size { get; set; }
    public required string StagingKey { get; set; }
    public required string ObjectKey { get; set; }
    public bool IsReady { get; set; }
    public bool IsDeleted { get; set; }
    /// <summary>Finalidad: "community" (adjuntos del tablón) o "exercise" (videos de la videoteca).</summary>
    public string Purpose { get; set; } = "community";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? PostId { get; set; }
    public Post? Post { get; set; }
    public int? VideoId { get; set; }
    public Video? Video { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Opinión verificada por reserva; conserva título y fecha si se elimina la clase.</summary>
public class SessionReview
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int? SessionId { get; set; }
    public Session? Session { get; set; }
    public required string SessionTitle { get; set; }
    public DateTime SessionDate { get; set; }
    public int Rating { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsHidden { get; set; }
}

/// <summary>Encuentro de la comunidad con cupo opcional e historial de cancelación.</summary>
public class CommunityEvent
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Location { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public int? Capacity { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<EventRegistration> Registrations { get; set; } = [];
}

/// <summary>Inscripción a un evento, única por usuario y evento.</summary>
public class EventRegistration
{
    public int EventId { get; set; }
    public CommunityEvent Event { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
