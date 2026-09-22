namespace CalisApi.Dtos;

/// <summary>Clase en vivo con cupos disponibles calculados.</summary>
public record SessionDto(
    int Id,
    string Title,
    string Description,
    DateTime Date,
    int LimitedSpots,
    int Enrolled,
    int AvailableSpots,
    string Difficulty,
    string CoachName,
    int DurationMinutes = 60);

/// <summary>Usuario inscrito a una clase (solo datos públicos).</summary>
public record EnrolledUserDto(int Id, string FullName, bool? Attended);

/// <summary>Marca de asistencia de inscritos a una clase (Admin).</summary>
public record RecordAttendanceRequest(int[] AttendedUserIds);

/// <summary>Estado del usuario actual respecto a una clase.</summary>
public static class SessionUserStatus
{
    public const string Enrolled = "enrolled";
    public const string Waitlist = "waitlist";
    public const string None = "none";
}

/// <summary>Detalle de clase: datos, participantes y estado del usuario autenticado.</summary>
public record SessionDetailsDto(
    SessionDto Session,
    IReadOnlyList<EnrolledUserDto> EnrolledUsers,
    string CurrentUserStatus);

/// <summary>Creación de clase (Admin).</summary>
public record CreateSessionRequest(
    string Title,
    string Description,
    DateTime Date,
    int LimitedSpots,
    string Difficulty,
    string CoachName,
    int DurationMinutes = 60,
    int[]? AchievementIds = null);
