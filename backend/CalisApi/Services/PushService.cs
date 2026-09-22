using System.Text.Json;
using CalisApi.Data;
using CalisApi.Dtos;
using Microsoft.EntityFrameworkCore;
using WebPush;

namespace CalisApi.Services;

/// <summary>
/// Notificaciones Web Push (VAPID). Gestiona suscripciones por dispositivo
/// y envía notificaciones a todos los dispositivos de un usuario.
/// </summary>
public class PushService(CalisDbContext db, IConfiguration configuration, ILogger<PushService> logger)
    : IPushService
{
    private string PublicKey => configuration["Push:VapidPublicKey"]
        ?? throw new InvalidOperationException("Falta Push:VapidPublicKey en la configuración.");
    private string PrivateKey => configuration["Push:VapidPrivateKey"]
        ?? throw new InvalidOperationException("Falta Push:VapidPrivateKey en la configuración.");
    private string Subject => configuration["Push:Subject"] ?? "mailto:admin@calisapp.com";

    /// <inheritdoc />
    public string GetPublicKey() => PublicKey;

    /// <inheritdoc />
    public async Task SubscribeAsync(int userId, PushSubscriptionRequest request, CancellationToken ct = default)
    {
        var existing = await db.PushSubscriptions
            .FirstOrDefaultAsync(p => p.Endpoint == request.Endpoint, ct);

        if (existing is null)
        {
            db.PushSubscriptions.Add(new Models.PushSubscription
            {
                UserId = userId,
                Endpoint = request.Endpoint,
                P256dh = request.Keys.P256dh,
                Auth = request.Keys.Auth,
            });
        }
        else
        {
            // El endpoint ya existía: actualizar claves y dueño (p. ej. re-login)
            existing.UserId = userId;
            existing.P256dh = request.Keys.P256dh;
            existing.Auth = request.Keys.Auth;
        }

        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task UnsubscribeAsync(string endpoint, CancellationToken ct = default)
    {
        var existing = await db.PushSubscriptions
            .FirstOrDefaultAsync(p => p.Endpoint == endpoint, ct);

        if (existing is not null)
        {
            db.PushSubscriptions.Remove(existing);
            await db.SaveChangesAsync(ct);
        }
    }

    /// <inheritdoc />
    public async Task SendToUserAsync(int userId, string title, string body, string url, CancellationToken ct = default)
    {
        var subscriptions = await db.PushSubscriptions
            .Where(p => p.UserId == userId)
            .ToListAsync(ct);

        if (subscriptions.Count == 0)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(new { title, body, url });
        var vapidDetails = new VapidDetails(Subject, PublicKey, PrivateKey);
        using var client = new WebPushClient();

        foreach (var sub in subscriptions)
        {
            try
            {
                var pushSubscription = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                await client.SendNotificationAsync(pushSubscription, payload, vapidDetails, ct);
            }
            catch (WebPushException ex) when (
                ex.StatusCode is System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.Gone)
            {
                // Suscripción expirada en el navegador: limpiarla
                db.PushSubscriptions.Remove(sub);
                logger.LogInformation("Suscripción push expirada eliminada (usuario {UserId})", userId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error enviando push a usuario {UserId}", userId);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}

/// <summary>Contrato del servicio de notificaciones push.</summary>
public interface IPushService
{
    string GetPublicKey();
    Task SubscribeAsync(int userId, PushSubscriptionRequest request, CancellationToken ct = default);
    Task UnsubscribeAsync(string endpoint, CancellationToken ct = default);
    Task SendToUserAsync(int userId, string title, string body, string url, CancellationToken ct = default);
}
