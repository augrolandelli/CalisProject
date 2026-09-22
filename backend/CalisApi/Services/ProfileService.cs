using CalisApi.Common;
using CalisApi.Data;
using CalisApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CalisApi.Services;

/// <summary>Perfil del usuario autenticado y sus estadísticas de progreso.</summary>
public class ProfileService(CalisDbContext db) : IProfileService
{
    /// <inheritdoc />
    public async Task<UserDto> GetAsync(int userId, CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw AppException.NotFound("El usuario no existe.");

        return new UserDto(user.Id, user.FullName, user.Phone, user.Email, user.Role, user.State, user.PhotoUrl);
    }

    /// <inheritdoc />
    public async Task<UserDto> UpdateAsync(int userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw AppException.NotFound("El usuario no existe.");

        user.FullName = request.FullName.Trim();
        user.Phone = request.Phone.Trim();
        user.PhotoUrl = string.IsNullOrWhiteSpace(request.PhotoUrl) ? null : request.PhotoUrl.Trim();

        await db.SaveChangesAsync(ct);

        return new UserDto(user.Id, user.FullName, user.Phone, user.Email, user.Role, user.State, user.PhotoUrl);
    }

    /// <inheritdoc />
    public async Task<UserStatsDto> GetStatsAsync(int userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var enrollments = db.UserSessions
            .AsNoTracking()
            .Where(e => e.UserId == userId);

        var endedEnrollments = enrollments.Where(e => e.Session.Date.AddMinutes(e.Session.DurationMinutes) < now);

        return new UserStatsDto(
            AchievementsCount: await db.UserAchievements.CountAsync(ua => ua.UserId == userId, ct),
            UpcomingClasses: await enrollments.CountAsync(e => e.Session.Date > now, ct),
            AttendedClasses: await endedEnrollments.CountAsync(e => e.Attended == true, ct),
            MissedClasses: await endedEnrollments.CountAsync(e => e.Attended == false, ct),
            ClassesThisMonth: await enrollments.CountAsync(e => e.Session.Date >= monthStart, ct));
    }
}

/// <summary>Contrato del servicio de perfil.</summary>
public interface IProfileService
{
    Task<UserDto> GetAsync(int userId, CancellationToken ct = default);
    Task<UserDto> UpdateAsync(int userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task<UserStatsDto> GetStatsAsync(int userId, CancellationToken ct = default);
}
