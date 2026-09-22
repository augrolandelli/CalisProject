namespace CalisApi.Dtos;

/// <summary>Logro del catálogo.</summary>
public record AchievementDto(int Id, string Name, string Description, string Icon);

/// <summary>Logro obtenido por un usuario (con fecha y clase de origen).</summary>
public record UserAchievementDto(
    int Id,
    int AchievementId,
    string Name,
    string Description,
    string Icon,
    DateTime DateEarned,
    int? SessionId);

/// <summary>Creación de logro (Admin).</summary>
public record CreateAchievementRequest(string Name, string Description, string Icon);

/// <summary>Otorgamiento de un logro a usuarios al finalizar una clase (Admin/Coach).</summary>
public record GrantAchievementRequest(int AchievementId, int SessionId, int[] UserIds);

/// <summary>Resultado del otorgamiento.</summary>
public record GrantAchievementResponse(int Granted, int Skipped);

/// <summary>Vista Admin del catálogo: incluye ocultos y cantidad de dueños.</summary>
public record AchievementAdminDto(
    int Id,
    string Name,
    string Description,
    string Icon,
    bool IsHidden,
    int GrantedCount);

/// <summary>Edición del perfil propio.</summary>
public record UpdateProfileRequest(string FullName, string Phone, string? PhotoUrl);

/// <summary>Estadísticas de progreso del usuario.</summary>
public record UserStatsDto(
    int AchievementsCount,
    int UpcomingClasses,
    int AttendedClasses,
    int MissedClasses,
    int ClassesThisMonth);
