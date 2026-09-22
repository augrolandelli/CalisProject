using CalisApi.Data;
using CalisApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CalisApi.Services;

/// <summary>
/// Revisa periódicamente clases finalizadas y notifica por push a los admins
/// para que marquen asistencia y otorguen logros. Notificación con 15 minutos de delay.
/// </summary>
public class SessionEndedNotificationService(IServiceProvider serviceProvider, ILogger<SessionEndedNotificationService> logger)
    : BackgroundService
{
    private static readonly TimeSpan DelayAfterSessionEnds = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SessionEndedNotificationService iniciado.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<CalisDbContext>();
                var pushService = scope.ServiceProvider.GetRequiredService<IPushService>();

                var cutoff = DateTime.UtcNow - DelayAfterSessionEnds;
                var sessionsToNotify = await db.Sessions
                    .AsNoTracking()
                    .Where(s => s.AdminNotifiedAt == null
                        && s.Date.AddMinutes(s.DurationMinutes) <= cutoff)
                    .ToListAsync(stoppingToken);

                var adminIds = await db.Users
                    .AsNoTracking()
                    .Where(u => u.Role == Roles.Admin)
                    .Select(u => u.Id)
                    .ToListAsync(stoppingToken);

                foreach (var session in sessionsToNotify)
                {
                    var tracked = await db.Sessions.FindAsync([session.Id], stoppingToken);
                    if (tracked is null) continue;

                    foreach (var adminId in adminIds)
                    {
                        try
                        {
                            await pushService.SendToUserAsync(
                                adminId,
                                "Clase finalizada",
                                $"\"{session.Title}\" terminó. Marcá asistencia y otorgá logros.",
                                $"/classes/{session.Id}",
                                stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "No se pudo notificar al admin {AdminId} por la sesión {SessionId}", adminId, session.Id);
                        }
                    }

                    tracked.AdminNotifiedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en SessionEndedNotificationService.");
            }

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
