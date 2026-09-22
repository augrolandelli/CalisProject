using CalisApi.Common;
using CalisApi.Data;
using CalisApi.Dtos;
using CalisApi.Models;
using CalisApi.Services.Storage;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CalisApi.Services;

/// <summary>Subida en dos pasos: staging firmado → validación del servidor → objeto privado inmutable.</summary>
public class MediaService(CalisDbContext db, IObjectStorage storage, IValidator<BeginUploadRequest> validator) : IMediaService
{
    // Acota la memoria del proceso durante la validación de clips de hasta 100 MB.
    private static readonly SemaphoreSlim ValidationSlots = new(2);

    public async Task<UploadDto> BeginAsync(int userId, BeginUploadRequest request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        var recent = DateTime.UtcNow.AddHours(-1);
        if (await db.MediaAssets.CountAsync(m => m.UploadedById == userId && m.CreatedAt > recent, ct) >= 50)
            throw new AppException("Alcanzaste el límite de 50 archivos por hora. Intenta más tarde.", 429);
        var id = Guid.NewGuid();
        var media = new MediaAsset
        {
            Id = id, UploadedById = userId, FileName = Path.GetFileName(request.FileName),
            ContentType = request.ContentType, Size = request.Size, Purpose = request.Purpose,
            StagingKey = $"staging/{id:N}",
            ObjectKey = $"{(request.Purpose == "exercise" ? "exercises" : "community")}/{id:N}",
        };
        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        var url = await storage.UploadUrlAsync(media.StagingKey, media.ContentType, expiresAt);
        db.MediaAssets.Add(media);
        await db.SaveChangesAsync(ct);
        return new(id, url, expiresAt);
    }

    public async Task<MediaDto> CompleteAsync(Guid id, int userId, CancellationToken ct)
    {
        await ValidationSlots.WaitAsync(ct);
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var media = await db.MediaAssets.FromSql($"SELECT * FROM MediaAssets WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id}")
                .FirstOrDefaultAsync(ct) ?? throw AppException.NotFound("El archivo no existe.");
            if (media.UploadedById != userId) throw AppException.Forbidden("La subida pertenece a otro usuario.");
            if (media.IsDeleted || media.CreatedAt < DateTime.UtcNow.AddDays(-1)) throw new AppException("La subida expiró. Selecciona el archivo nuevamente.");
            if (!media.IsReady)
            {
                var bytes = await storage.ReadAsync(media.StagingKey, media.Size, ct);
                // Videos de ejercicios: máximo 30 segundos (decisión Fase 5); clips de comunidad: 120.
                MediaFileValidator.Validate(bytes, media.ContentType, media.Purpose == "exercise" ? 30 : 120);
                // Se guardan los bytes validados, no una copia de una clave staging aún modificable.
                await storage.WriteAsync(media.ObjectKey, bytes, media.ContentType, ct);
                media.IsReady = true;
                await db.SaveChangesAsync(ct);
            }
            await tx.CommitAsync(ct);
            // El original de staging ya no se necesita: el objeto validado es inmutable.
            // Best-effort fuera de la transacción; si falla, lo limpia el proceso horario.
            try { await storage.DeleteAsync(media.StagingKey, CancellationToken.None); }
            catch { /* el proceso de limpieza lo reintentará */ }
            return new(media.Id, media.FileName, media.ContentType, media.Size);
        }
        finally { ValidationSlots.Release(); }
    }

    public async Task<MediaLinkDto> LinkAsync(Guid id, int userId, bool admin, CancellationToken ct)
    {
        var media = await db.MediaAssets.AsNoTracking().Where(m => m.Id == id && !m.IsDeleted && m.IsReady
            && ((m.PostId != null && !m.Post!.IsDeleted) || m.VideoId != null
                || (admin && m.UploadedById == userId && m.CreatedAt > DateTime.UtcNow.AddDays(-1))))
            .FirstOrDefaultAsync(ct) ?? throw AppException.NotFound("El archivo no está disponible.");
        var expiresAt = DateTime.UtcNow.AddMinutes(5);
        return new(await storage.ReadUrlAsync(media.ObjectKey, expiresAt), expiresAt);
    }

    public async Task AbandonAsync(Guid id, int userId, CancellationToken ct)
    {
        await db.MediaAssets.Where(m => m.Id == id && m.UploadedById == userId && m.PostId == null)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsDeleted, true), ct);
    }
}

public interface IMediaService
{
    Task<UploadDto> BeginAsync(int userId, BeginUploadRequest request, CancellationToken ct);
    Task<MediaDto> CompleteAsync(Guid id, int userId, CancellationToken ct);
    Task<MediaLinkDto> LinkAsync(Guid id, int userId, bool admin, CancellationToken ct);
    Task AbandonAsync(Guid id, int userId, CancellationToken ct);
}

/// <summary>Limpieza reintentable de objetos retirados y subidas abandonadas; no elimina filas antes de borrar R2.</summary>
public class MediaCleanupService(IServiceScopeFactory scopes, ILogger<MediaCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CalisDbContext>();
                var storage = scope.ServiceProvider.GetRequiredService<IObjectStorage>();
                if (!storage.IsConfigured) continue;
                var cutoff = DateTime.UtcNow.AddDays(-1);
                // Se limpian retirados y archivos sin asociar (ni a posts ni a videos de ejercicios).
                var ids = await db.MediaAssets.Where(m => m.CreatedAt < cutoff
                        && (m.IsDeleted || (m.PostId == null && m.VideoId == null)))
                    .OrderBy(m => m.CreatedAt).Select(m => m.Id).Take(100).ToListAsync(stoppingToken);
                foreach (var id in ids)
                {
                    await using var tx = await db.Database.BeginTransactionAsync(stoppingToken);
                    var media = await db.MediaAssets.FromSql($"SELECT * FROM MediaAssets WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id}")
                        .FirstOrDefaultAsync(stoppingToken);
                    if (media is not null && (media.IsDeleted || (media.PostId == null && media.VideoId == null)))
                    {
                        await storage.DeleteAsync(media.ObjectKey, stoppingToken);
                        await storage.DeleteAsync(media.StagingKey, stoppingToken);
                        db.MediaAssets.Remove(media);
                        await db.SaveChangesAsync(stoppingToken);
                    }
                    await tx.CommitAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogWarning(ex, "La limpieza de archivos se reintentará en el próximo ciclo"); }
        }
    }
}
