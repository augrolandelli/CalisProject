using CalisApi.Common;
using CalisApi.Data;
using CalisApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CalisApi.Services;

/// <summary>
/// Inscripciones a clases con cupos limitados.
/// Reglas críticas (spec §4.3): transacción + bloqueo de fila (UPDLOCK) +
/// constraint única (UserId, SessionId) — anti-overbooking y anti doble inscripción.
/// </summary>
public class UserSessionService(CalisDbContext db, IPushService pushService, ILogger<UserSessionService> logger)
    : IUserSessionService
{
    /// <inheritdoc />
    public async Task EnrollAsync(int sessionId, int userId, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        // Bloqueo de la fila de la sesión durante la transacción
        var session = await LockSessionAsync(sessionId, ct);
        EnsureNotStarted(session);

        var alreadyEnrolled = await db.UserSessions
            .AnyAsync(e => e.SessionId == sessionId && e.UserId == userId, ct);
        if (alreadyEnrolled)
        {
            throw new AppException("Ya estás inscrito en esta clase.");
        }

        if (session.Enrolled >= session.LimitedSpots)
        {
            throw new AppException("No quedan cupos disponibles. Puedes unirte a la lista de espera.");
        }

        db.UserSessions.Add(new UserSession { SessionId = sessionId, UserId = userId });
        session.Enrolled++;

        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Fallback de concurrencia: la constraint única rechazó un duplicado
            throw new AppException("Ya estás inscrito en esta clase.");
        }
    }

    /// <inheritdoc />
    public async Task UnenrollAsync(int sessionId, int userId, CancellationToken ct = default)
    {
        int? promotedUserId = null;
        string? sessionTitle = null;

        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            var session = await LockSessionAsync(sessionId, ct);
            EnsureNotStarted(session);
            sessionTitle = session.Title;

            var enrollment = await db.UserSessions
                .FirstOrDefaultAsync(e => e.SessionId == sessionId && e.UserId == userId, ct)
                ?? throw new AppException("No estás inscrito en esta clase.");

            db.UserSessions.Remove(enrollment);
            session.Enrolled--;

            // Si hay lista de espera, el primero entra automáticamente al cupo liberado
            var nextInLine = await db.SessionWaitlists
                .Where(w => w.SessionId == sessionId)
                .OrderBy(w => w.Position)
                .FirstOrDefaultAsync(ct);

            if (nextInLine is not null)
            {
                db.SessionWaitlists.Remove(nextInLine);
                db.UserSessions.Add(new UserSession { SessionId = sessionId, UserId = nextInLine.UserId });
                session.Enrolled++;
                promotedUserId = nextInLine.UserId;
            }

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }

        // Push fuera de la transacción: si falla no afecta la reserva
        if (promotedUserId is not null)
        {
            try
            {
                await pushService.SendToUserAsync(
                    promotedUserId.Value,
                    "¡Se liberó tu cupo!",
                    $"Ya tienes un lugar confirmado en \"{sessionTitle}\".",
                    "/classes",
                    ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "No se pudo enviar push al usuario {UserId}", promotedUserId);
            }
        }
    }

    /// <inheritdoc />
    public async Task JoinWaitlistAsync(int sessionId, int userId, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var session = await LockSessionAsync(sessionId, ct);
        EnsureNotStarted(session);

        var alreadyEnrolled = await db.UserSessions
            .AnyAsync(e => e.SessionId == sessionId && e.UserId == userId, ct);
        if (alreadyEnrolled)
        {
            throw new AppException("Ya estás inscrito en esta clase.");
        }

        if (session.Enrolled < session.LimitedSpots)
        {
            throw new AppException("La clase tiene cupos disponibles, puedes reservar directamente.");
        }

        var alreadyWaiting = await db.SessionWaitlists
            .AnyAsync(w => w.SessionId == sessionId && w.UserId == userId, ct);
        if (alreadyWaiting)
        {
            throw new AppException("Ya estás en la lista de espera.");
        }

        var nextPosition = await db.SessionWaitlists
            .Where(w => w.SessionId == sessionId)
            .Select(w => (int?)w.Position)
            .MaxAsync(ct) ?? 0;

        db.SessionWaitlists.Add(new SessionWaitlist
        {
            SessionId = sessionId,
            UserId = userId,
            Position = nextPosition + 1,
        });

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    /// <inheritdoc />
    public async Task LeaveWaitlistAsync(int sessionId, int userId, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        EnsureNotStarted(await LockSessionAsync(sessionId, ct));
        var entry = await db.SessionWaitlists
            .FirstOrDefaultAsync(w => w.SessionId == sessionId && w.UserId == userId, ct)
            ?? throw new AppException("No estás en la lista de espera de esta clase.");

        db.SessionWaitlists.Remove(entry);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private static void EnsureNotStarted(Session session)
    {
        if (session.Date <= DateTime.UtcNow)
            throw new AppException("La clase ya comenzó. Las reservas y cancelaciones están cerradas.");
    }

    /// <summary>Bloquea la fila de la sesión (UPDLOCK, HOLDLOCK) y la devuelve trackeada.</summary>
    private async Task<Session> LockSessionAsync(int sessionId, CancellationToken ct) =>
        await db.Sessions
            .FromSql($"SELECT * FROM Sessions WITH (UPDLOCK, HOLDLOCK) WHERE Id = {sessionId}")
            .FirstOrDefaultAsync(ct)
        ?? throw AppException.NotFound("La sesión no existe.");
}

/// <summary>Contrato del servicio de inscripciones.</summary>
public interface IUserSessionService
{
    Task EnrollAsync(int sessionId, int userId, CancellationToken ct = default);
    Task UnenrollAsync(int sessionId, int userId, CancellationToken ct = default);
    Task JoinWaitlistAsync(int sessionId, int userId, CancellationToken ct = default);
    Task LeaveWaitlistAsync(int sessionId, int userId, CancellationToken ct = default);
}
