using CalisApi.Common;
using CalisApi.Data;
using CalisApi.Dtos;
using CalisApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CalisApi.Services;

/// <summary>
/// Logros: catálogo, consultas por usuario/clase y otorgamiento al finalizar clases.
/// </summary>
public class AchievementService(CalisDbContext db) : IAchievementService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AchievementDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.Achievements
            .AsNoTracking()
            .Where(a => !a.IsHidden)
            .OrderBy(a => a.Name)
            .Select(a => new AchievementDto(a.Id, a.Name, a.Description, a.Icon))
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AchievementAdminDto>> GetAllForAdminAsync(CancellationToken ct = default) =>
        await db.Achievements
            .AsNoTracking()
            .OrderBy(a => a.IsHidden)
            .ThenBy(a => a.Name)
            .Select(a => new AchievementAdminDto(
                a.Id, a.Name, a.Description, a.Icon, a.IsHidden, a.UserAchievements.Count))
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<AchievementDto> UpdateAsync(int id, CreateAchievementRequest request, CancellationToken ct = default)
    {
        var achievement = await db.Achievements.FindAsync([id], ct)
            ?? throw AppException.NotFound("El logro no existe.");

        achievement.Name = request.Name.Trim();
        achievement.Description = request.Description.Trim();
        achievement.Icon = request.Icon.Trim();
        await db.SaveChangesAsync(ct);

        return new AchievementDto(achievement.Id, achievement.Name, achievement.Description, achievement.Icon);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var achievement = await db.Achievements.FindAsync([id], ct)
            ?? throw AppException.NotFound("El logro no existe.");

        var granted = await db.UserAchievements.CountAsync(ua => ua.AchievementId == id, ct);
        if (granted > 0)
        {
            // Ya tiene dueños: se oculta del catálogo y el historial se conserva.
            achievement.IsHidden = true;
        }
        else
        {
            // Nadie lo ganó: borrado físico (las asociaciones a clases caen en cascada).
            db.Achievements.Remove(achievement);
        }
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> GetUserIdsAsync(int achievementId, CancellationToken ct = default)
    {
        if (!await db.Achievements.AnyAsync(a => a.Id == achievementId, ct))
        {
            throw AppException.NotFound("El logro no existe.");
        }

        return await db.UserAchievements
            .AsNoTracking()
            .Where(ua => ua.AchievementId == achievementId)
            .Select(ua => ua.UserId)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserAchievementDto>> GetForUserAsync(int userId, CancellationToken ct = default)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId, ct))
        {
            throw AppException.NotFound("El usuario no existe.");
        }

        return await db.UserAchievements
            .AsNoTracking()
            .Where(ua => ua.UserId == userId)
            .OrderByDescending(ua => ua.DateEarned)
            .Select(ua => new UserAchievementDto(
                ua.Id, ua.AchievementId, ua.Achievement.Name, ua.Achievement.Description,
                ua.Achievement.Icon, ua.DateEarned, ua.SessionId))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AchievementDto>> GetForSessionAsync(int sessionId, CancellationToken ct = default)
    {
        if (!await db.Sessions.AnyAsync(s => s.Id == sessionId, ct))
        {
            throw AppException.NotFound("La sesión no existe.");
        }

        return await db.SessionAchievements
            .AsNoTracking()
            .Where(sa => sa.SessionId == sessionId)
            .Select(sa => new AchievementDto(
                sa.Achievement.Id, sa.Achievement.Name, sa.Achievement.Description, sa.Achievement.Icon))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<AchievementDto> CreateAsync(CreateAchievementRequest request, CancellationToken ct = default)
    {
        var achievement = new Achievement
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Icon = request.Icon.Trim(),
        };

        db.Achievements.Add(achievement);
        await db.SaveChangesAsync(ct);

        return new AchievementDto(achievement.Id, achievement.Name, achievement.Description, achievement.Icon);
    }

    /// <inheritdoc />
    public async Task AssociateToSessionAsync(int sessionId, int achievementId, CancellationToken ct = default)
    {
        if (!await db.Sessions.AnyAsync(s => s.Id == sessionId, ct))
        {
            throw AppException.NotFound("La sesión no existe.");
        }
        if (!await db.Achievements.AnyAsync(a => a.Id == achievementId, ct))
        {
            throw AppException.NotFound("El logro no existe.");
        }

        var exists = await db.SessionAchievements
            .AnyAsync(sa => sa.SessionId == sessionId && sa.AchievementId == achievementId, ct);
        if (exists)
        {
            throw AppException.Conflict("El logro ya está asociado a esta clase.");
        }

        db.SessionAchievements.Add(new SessionAchievement
        {
            SessionId = sessionId,
            AchievementId = achievementId,
        });
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<GrantAchievementResponse> GrantAsync(GrantAchievementRequest request, CancellationToken ct = default)
    {
        if (request.UserIds is not { Length: > 0 and <= 500 })
            throw new AppException("Selecciona entre 1 y 500 participantes.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var session = await db.Sessions.FromSql($"SELECT * FROM Sessions WITH (UPDLOCK, HOLDLOCK) WHERE Id = {request.SessionId}")
            .FirstOrDefaultAsync(ct) ?? throw AppException.NotFound("La sesión no existe.");
        if (session.Date.AddMinutes(session.DurationMinutes) > DateTime.UtcNow)
            throw new AppException("Los logros se otorgan cuando termina la clase.");
        var achievement = await db.Achievements.FirstOrDefaultAsync(a => a.Id == request.AchievementId, ct)
            ?? throw AppException.NotFound("El logro no existe.");
        if (achievement.IsHidden)
            throw new AppException("El logro está oculto del catálogo y no se puede otorgar.");
        if (!await db.SessionAchievements.AnyAsync(a => a.SessionId == request.SessionId && a.AchievementId == request.AchievementId, ct))
            throw new AppException("Este logro no está asociado a la clase.");

        var userIds = request.UserIds.Distinct().ToArray();
        var existingUserIds = await db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync(ct);
        if (existingUserIds.Count != userIds.Length)
        {
            throw AppException.NotFound("Uno o más usuarios no existen.");
        }
        var enrolled = await db.UserSessions
            .Where(e => e.SessionId == request.SessionId && userIds.Contains(e.UserId))
            .ToListAsync(ct);
        if (enrolled.Count != userIds.Length)
            throw new AppException("Solo puedes otorgar logros a los participantes inscritos.");
        if (enrolled.Any(e => e.Attended != true))
            throw new AppException("Solo puedes otorgar logros a los asistentes. Marcá la asistencia primero.");

        // No duplicar: si ya tiene ese logro (de cualquier clase), se saltea
        var alreadyGranted = await db.UserAchievements
            .Where(ua => userIds.Contains(ua.UserId) && ua.AchievementId == request.AchievementId)
            .Select(ua => ua.UserId)
            .ToListAsync(ct);

        var toGrant = userIds.Except(alreadyGranted).ToArray();
        var clovers = await db.Users.Where(u => toGrant.Contains(u.Id) && u.Role == Roles.Clover)
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
        foreach (var userId in toGrant)
        {
            var earned = new UserAchievement
            {
                UserId = userId,
                AchievementId = request.AchievementId,
                SessionId = request.SessionId,
            };
            db.UserAchievements.Add(earned);
            if (clovers.TryGetValue(userId, out var name))
                db.Posts.Add(new Post
                {
                    Title = $"Nuevo logro: {achievement.Name}",
                    Content = $"{name} consiguió {achievement.Name} en la clase {session.Title}. ¡Felicitaciones!",
                    Kind = PostKinds.Achievement, AuthorId = userId, UserAchievement = earned,
                });
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new GrantAchievementResponse(toGrant.Length, alreadyGranted.Count);
    }
}

/// <summary>Contrato del servicio de logros.</summary>
public interface IAchievementService
{
    Task<IReadOnlyList<AchievementDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AchievementAdminDto>> GetAllForAdminAsync(CancellationToken ct = default);
    Task<AchievementDto> UpdateAsync(int id, CreateAchievementRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<int>> GetUserIdsAsync(int achievementId, CancellationToken ct = default);
    Task<IReadOnlyList<UserAchievementDto>> GetForUserAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<AchievementDto>> GetForSessionAsync(int sessionId, CancellationToken ct = default);
    Task<AchievementDto> CreateAsync(CreateAchievementRequest request, CancellationToken ct = default);
    Task AssociateToSessionAsync(int sessionId, int achievementId, CancellationToken ct = default);
    Task<GrantAchievementResponse> GrantAsync(GrantAchievementRequest request, CancellationToken ct = default);
}
