using CalisApi.Common;
using CalisApi.Data;
using CalisApi.Dtos;
using CalisApi.Models;
using CalisApi.Services.Storage;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CalisApi.Services;

/// <summary>Videoteca de ejercicios: lectura con filtros y gestión Admin (Fase 5).</summary>
public class VideoService(
    CalisDbContext db,
    IObjectStorage storage,
    IValidator<WriteVideoRequest> validator) : IVideoService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<VideoDto>> GetAllAsync(
        int? categoryId, string? searchTerm, CancellationToken ct = default)
    {
        var query = db.Videos.AsNoTracking().Include(v => v.Category).AsQueryable();

        if (categoryId is not null)
        {
            query = query.Where(v => v.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(v =>
                v.Title.ToLower().Contains(term) || v.Description.ToLower().Contains(term));
        }

        return await query
            .OrderBy(v => v.Title)
            .Select(ToDto)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<VideoDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var video = await db.Videos
            .AsNoTracking()
            .Include(v => v.Category)
            .FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw AppException.NotFound("El video no existe.");

        var playbackUrl = await PlaybackUrlAsync(video.Id, ct);
        return new VideoDto(
            video.Id, video.Title, video.Description, video.Difficulty, video.Requisites,
            playbackUrl ?? video.Url, video.CategoryId,
            new CategoryDto(video.Category.Id, video.Category.Name, video.Category.Description));
    }

    /// <inheritdoc />
    public async Task<VideoDto> CreateAsync(int adminUserId, WriteVideoRequest request, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        if (request.MediaAssetId is null)
            throw new AppException("Sube el archivo de video antes de guardar.");
        await ValidateCategoryAsync(request.CategoryId, ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var media = await LockAssetAsync(request.MediaAssetId.Value, ct);
        ValidateAssetForVideo(media, adminUserId);

        var video = new Video
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Difficulty = request.Difficulty.Trim().ToLowerInvariant(),
            Requisites = request.Requisites.Trim(),
            Url = "", // el archivo vive en el almacenamiento; la URL de reproducción se firma al consultar
            CategoryId = request.CategoryId,
        };
        db.Videos.Add(video);
        media.Video = video;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await GetByIdAsync(video.Id, ct);
    }

    /// <inheritdoc />
    public async Task<VideoDto> UpdateAsync(int id, int adminUserId, WriteVideoRequest request, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        await ValidateCategoryAsync(request.CategoryId, ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var video = await db.Videos.FromSql($"SELECT * FROM Videos WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id}")
            .FirstOrDefaultAsync(ct) ?? throw AppException.NotFound("El video no existe.");

        if (request.MediaAssetId is not null)
        {
            var media = await LockAssetAsync(request.MediaAssetId.Value, ct);
            ValidateAssetForVideo(media, adminUserId);
            // Reemplazo de archivo: el anterior queda retirado para limpieza.
            await db.MediaAssets.Where(m => m.VideoId == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(m => m.IsDeleted, true)
                    .SetProperty(m => m.VideoId, (int?)null), ct);
            media.Video = video;
        }

        video.Title = request.Title.Trim();
        video.Description = request.Description.Trim();
        video.Difficulty = request.Difficulty.Trim().ToLowerInvariant();
        video.Requisites = request.Requisites.Trim();
        video.CategoryId = request.CategoryId;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await GetByIdAsync(video.Id, ct);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var video = await db.Videos.FromSql($"SELECT * FROM Videos WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id}")
            .FirstOrDefaultAsync(ct) ?? throw AppException.NotFound("El video no existe.");

        var references = await db.RutineExercises.CountAsync(e => e.VideoId == id, ct);
        if (references > 0)
            throw new AppException($"No se puede eliminar: {references} ejercicio(s) de rutinas usan este video. Quítalo de esas rutinas primero.");

        // El archivo queda retirado; el proceso de limpieza borra el objeto del almacenamiento.
        await db.MediaAssets.Where(m => m.VideoId == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.IsDeleted, true)
                .SetProperty(m => m.VideoId, (int?)null), ct);

        db.Videos.Remove(video);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private async Task ValidateCategoryAsync(int categoryId, CancellationToken ct)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == categoryId, ct))
            throw AppException.NotFound("La categoría no existe.");
    }

    private async Task<MediaAsset> LockAssetAsync(Guid mediaId, CancellationToken ct) =>
        await db.MediaAssets.FromSql($"SELECT * FROM MediaAssets WITH (UPDLOCK, HOLDLOCK) WHERE Id = {mediaId}")
            .FirstOrDefaultAsync(ct) ?? throw AppException.NotFound("El archivo no existe.");

    private static void ValidateAssetForVideo(MediaAsset media, int adminUserId)
    {
        if (!media.IsReady || media.IsDeleted || media.Purpose != "exercise"
            || media.PostId is not null || media.VideoId is not null
            || media.UploadedById != adminUserId || media.CreatedAt < DateTime.UtcNow.AddDays(-1))
            throw new AppException("El archivo no está disponible. Sube el video nuevamente.");
    }

    private async Task<string?> PlaybackUrlAsync(int videoId, CancellationToken ct)
    {
        var asset = await db.MediaAssets.AsNoTracking()
            .FirstOrDefaultAsync(m => m.VideoId == videoId && m.IsReady && !m.IsDeleted, ct);
        return asset is null ? null : await storage.ReadUrlAsync(asset.ObjectKey, DateTime.UtcNow.AddMinutes(30));
    }

    private static System.Linq.Expressions.Expression<System.Func<Video, VideoDto>> ToDto =>
        v => new VideoDto(
            v.Id, v.Title, v.Description, v.Difficulty, v.Requisites, v.Url, v.CategoryId,
            new CategoryDto(v.Category.Id, v.Category.Name, v.Category.Description));
}

/// <summary>Contrato del servicio de videos.</summary>
public interface IVideoService
{
    Task<IReadOnlyList<VideoDto>> GetAllAsync(int? categoryId, string? searchTerm, CancellationToken ct = default);
    Task<VideoDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<VideoDto> CreateAsync(int adminUserId, WriteVideoRequest request, CancellationToken ct = default);
    Task<VideoDto> UpdateAsync(int id, int adminUserId, WriteVideoRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
