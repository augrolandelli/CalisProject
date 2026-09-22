using CalisApi.Common;
using CalisApi.Data;
using CalisApi.Dtos;
using CalisApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CalisApi.Services;

/// <summary>Clases en vivo: lectura y gestión Admin (crear, editar con aviso push, eliminar, borrado por mes).</summary>
public class SessionService(CalisDbContext db, IPushService pushService, ILogger<SessionService> logger) : ISessionService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SessionDto>> GetAllAsync(DateTime? date, CancellationToken ct = default)
    {
        var query = db.Sessions.AsNoTracking().AsQueryable();

        if (date is not null)
        {
            var dayStart = date.Value.Date;
            var dayEnd = dayStart.AddDays(1);
            query = query.Where(s => s.Date >= dayStart && s.Date < dayEnd);
        }

        return await query
            .OrderBy(s => s.Date)
            .Select(ToDto)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<SessionDto> GetByIdAsync(int id, CancellationToken ct = default) =>
        await db.Sessions
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(ct)
        ?? throw AppException.NotFound("La sesión no existe.");

    /// <inheritdoc />
    public async Task<SessionDetailsDto> GetDetailsAsync(int id, int? currentUserId, CancellationToken ct = default)
    {
        var session = await db.Sessions
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(ct)
            ?? throw AppException.NotFound("La sesión no existe.");

        var enrolledUsers = await db.UserSessions
            .AsNoTracking()
            .Where(e => e.SessionId == id)
            .OrderBy(e => e.CreatedAt)
            .Select(e => new EnrolledUserDto(e.UserId, e.User.FullName, e.Attended))
            .ToListAsync(ct);

        var status = SessionUserStatus.None;
        if (currentUserId is not null)
        {
            if (enrolledUsers.Any(u => u.Id == currentUserId))
            {
                status = SessionUserStatus.Enrolled;
            }
            else if (await db.SessionWaitlists.AnyAsync(
                w => w.SessionId == id && w.UserId == currentUserId, ct))
            {
                status = SessionUserStatus.Waitlist;
            }
        }

        return new SessionDetailsDto(session, enrolledUsers, status);
    }

    /// <inheritdoc />
    public async Task<SessionDto> CreateAsync(CreateSessionRequest request, CancellationToken ct = default)
    {
        if (request.DurationMinutes is < 15 or > 480)
            throw new AppException("La duración debe estar entre 15 y 480 minutos.");
        var session = new Session
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Date = request.Date.ToUniversalTime(),
            DurationMinutes = request.DurationMinutes,
            LimitedSpots = request.LimitedSpots,
            Enrolled = 0,
            Difficulty = request.Difficulty.Trim().ToLowerInvariant(),
            CoachName = request.CoachName.Trim(),
        };

        db.Sessions.Add(session);
        await db.SaveChangesAsync(ct);

        await SyncSessionAchievementsAsync(session.Id, request.AchievementIds, ct);

        return await GetByIdAsync(session.Id, ct);
    }

    /// <inheritdoc />
    public async Task<SessionDto> UpdateAsync(int id, CreateSessionRequest request, CancellationToken ct = default)
    {
        if (request.DurationMinutes is < 15 or > 480)
            throw new AppException("La duración debe estar entre 15 y 480 minutos.");

        List<int> enrolledUserIds = [];
        string? notifyTitle = null;
        DateTime newDateUtc = request.Date.ToUniversalTime();

        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            var session = await db.Sessions
                .FromSql($"SELECT * FROM Sessions WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id}")
                .FirstOrDefaultAsync(ct) ?? throw AppException.NotFound("La sesión no existe.");

            if (session.Date <= DateTime.UtcNow)
                throw new AppException("La clase ya comenzó; no se puede editar.");
            if (request.LimitedSpots < session.Enrolled)
                throw new AppException($"El cupo no puede ser menor que los inscritos actuales ({session.Enrolled}).");

            if (session.Date != newDateUtc && session.Enrolled > 0)
            {
                notifyTitle = session.Title;
                enrolledUserIds = await db.UserSessions
                    .Where(e => e.SessionId == id)
                    .Select(e => e.UserId)
                    .ToListAsync(ct);
            }

            session.Title = request.Title.Trim();
            session.Description = request.Description.Trim();
            session.Date = newDateUtc;
            session.DurationMinutes = request.DurationMinutes;
            session.LimitedSpots = request.LimitedSpots;
            session.Difficulty = request.Difficulty.Trim().ToLowerInvariant();
            session.CoachName = request.CoachName.Trim();

            await db.SaveChangesAsync(ct);
            await SyncSessionAchievementsAsync(id, request.AchievementIds, ct);
            await transaction.CommitAsync(ct);
        }

        // Push fuera de la transacción: si falla no afecta la edición.
        if (notifyTitle is not null)
        {
            foreach (var userId in enrolledUserIds)
            {
                try
                {
                    await pushService.SendToUserAsync(
                        userId,
                        "Cambio de horario",
                        $"La clase \"{notifyTitle}\" se movió al {newDateUtc.ToLocalTime():d 'de' MMMM 'a las' HH:mm}.",
                        $"/classes/{id}",
                        ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "No se pudo avisar del cambio de horario al usuario {UserId}", userId);
                }
            }
        }

        return await GetByIdAsync(id, ct);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var session = await db.Sessions.FindAsync([id], ct)
            ?? throw AppException.NotFound("La sesión no existe.");

        db.Sessions.Remove(session);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public Task<int> CountByMonthAsync(int year, int month, CancellationToken ct = default)
    {
        ValidatePastMonth(year, month);
        var (start, end) = MonthRange(year, month);
        return db.Sessions.CountAsync(s => s.Date >= start && s.Date < end, ct);
    }

    /// <inheritdoc />
    public async Task RecordAttendanceAsync(int id, RecordAttendanceRequest request, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var session = await db.Sessions
            .FromSql($"SELECT * FROM Sessions WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id}")
            .FirstOrDefaultAsync(ct) ?? throw AppException.NotFound("La sesión no existe.");

        if (session.Date.AddMinutes(session.DurationMinutes) > DateTime.UtcNow)
            throw new AppException("La clase no terminó. La asistencia se marca al finalizar.");

        var attendedIds = request.AttendedUserIds.Distinct().ToHashSet();
        var enrollments = await db.UserSessions
            .Where(e => e.SessionId == id)
            .ToListAsync(ct);

        if (attendedIds.Any(id => !enrollments.Any(e => e.UserId == id)))
            throw new AppException("Solo se puede marcar asistencia de usuarios inscritos.");

        foreach (var enrollment in enrollments)
        {
            enrollment.Attended = attendedIds.Contains(enrollment.UserId);
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    /// <inheritdoc />
    public async Task<int> DeleteByMonthAsync(int year, int month, CancellationToken ct = default)
    {
        ValidatePastMonth(year, month);
        var (start, end) = MonthRange(year, month);
        // DELETE a nivel SQL: las FK en cascada/SetNull de la BD limpian inscripciones,
        // waitlist, logros de clase y conservan reseñas y logros de usuarios.
        return await db.Sessions
            .Where(s => s.Date >= start && s.Date < end)
            .ExecuteDeleteAsync(ct);
    }

    private static void ValidatePastMonth(int year, int month)
    {
        if (month is < 1 or > 12 || year is < 2000 or > 2100)
            throw new AppException("Mes inválido. Usa el formato year + month (ej. 2026, 8).");
        var now = DateTime.UtcNow;
        if (year > now.Year || (year == now.Year && month >= now.Month))
            throw new AppException("Solo se pueden eliminar clases de meses ya finalizados.");
    }

    private static (DateTime Start, DateTime End) MonthRange(int year, int month) =>
        (new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc),
         new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1));

    private static System.Linq.Expressions.Expression<System.Func<Session, SessionDto>> ToDto =>
        s => new SessionDto(
            s.Id, s.Title, s.Description, s.Date, s.LimitedSpots, s.Enrolled,
            s.LimitedSpots - s.Enrolled, s.Difficulty, s.CoachName, s.DurationMinutes);

    private async Task SyncSessionAchievementsAsync(int sessionId, int[]? achievementIds, CancellationToken ct)
    {
        if (achievementIds is null || achievementIds.Length == 0)
        {
            var existing = await db.SessionAchievements
                .Where(sa => sa.SessionId == sessionId)
                .ToListAsync(ct);
            db.SessionAchievements.RemoveRange(existing);
            await db.SaveChangesAsync(ct);
            return;
        }

        var distinctIds = achievementIds.Distinct().ToArray();
        var validAchievements = await db.Achievements
            .Where(a => distinctIds.Contains(a.Id) && !a.IsHidden)
            .Select(a => a.Id)
            .ToListAsync(ct);

        if (validAchievements.Count != distinctIds.Length)
        {
            throw new AppException("Uno o más logros no existen o están ocultos.");
        }

        var current = await db.SessionAchievements
            .Where(sa => sa.SessionId == sessionId)
            .ToListAsync(ct);

        var toRemove = current.Where(sa => !distinctIds.Contains(sa.AchievementId)).ToList();
        var existingIds = current.Select(sa => sa.AchievementId).ToHashSet();
        var toAdd = distinctIds
            .Where(id => !existingIds.Contains(id))
            .Select(id => new SessionAchievement { SessionId = sessionId, AchievementId = id })
            .ToList();

        db.SessionAchievements.RemoveRange(toRemove);
        db.SessionAchievements.AddRange(toAdd);
        await db.SaveChangesAsync(ct);
    }
}

/// <summary>Contrato del servicio de sesiones.</summary>
public interface ISessionService
{
    Task<IReadOnlyList<SessionDto>> GetAllAsync(DateTime? date, CancellationToken ct = default);
    Task<SessionDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<SessionDetailsDto> GetDetailsAsync(int id, int? currentUserId, CancellationToken ct = default);
    Task<SessionDto> CreateAsync(CreateSessionRequest request, CancellationToken ct = default);
    Task<SessionDto> UpdateAsync(int id, CreateSessionRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task RecordAttendanceAsync(int id, RecordAttendanceRequest request, CancellationToken ct = default);
    Task<int> CountByMonthAsync(int year, int month, CancellationToken ct = default);
    Task<int> DeleteByMonthAsync(int year, int month, CancellationToken ct = default);
}
