using CalisApi.Common;
using CalisApi.Data;
using CalisApi.Dtos;
using CalisApi.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CalisApi.Services;

/// <summary>Tablón, adjuntos y reacciones. Las mutaciones se serializan por publicación.</summary>
public class PostService(CalisDbContext db, IValidator<WritePostRequest> validator) : IPostService
{
    public Task<PageDto<PostDto>> ListAsync(int userId, int page, int pageSize, CancellationToken ct) =>
        Project(db.Posts.Where(p => !p.IsDeleted).OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id), userId).PageAsync(page, pageSize, ct);

    public async Task<PostDto> GetAsync(int id, int userId, CancellationToken ct) =>
        await Project(db.Posts.Where(p => !p.IsDeleted && p.Id == id), userId).FirstOrDefaultAsync(ct)
        ?? throw AppException.NotFound("La publicación no existe.");

    public async Task<PostDto> SaveAsync(int? id, int userId, WritePostRequest request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var post = id.HasValue ? await LockAsync(id.Value, ct) : new Post
        {
            Title = request.Title.Trim(), Content = request.Content.Trim(), AuthorId = userId,
        };
        if (post.Kind == PostKinds.Achievement)
            throw new AppException("Los logros automáticos se pueden retirar, pero no editar.");

        var previousIds = id.HasValue
            ? await db.MediaAssets.Where(m => m.PostId == id && !m.IsDeleted).Select(m => m.Id).ToListAsync(ct)
            : [];
        var selected = new List<MediaAsset>();
        // Orden estable para evitar deadlocks entre editores y el limpiador de archivos.
        foreach (var mediaId in previousIds.Union(request.MediaIds).Order())
        {
            var media = await db.MediaAssets.FromSql($"SELECT * FROM MediaAssets WITH (UPDLOCK, HOLDLOCK) WHERE Id = {mediaId}")
                .FirstOrDefaultAsync(ct) ?? throw AppException.NotFound("Un adjunto ya no existe.");
            if (request.MediaIds.Contains(mediaId))
            {
                if (!media.IsReady || media.IsDeleted || media.Purpose != "community" ||
                    (media.PostId.HasValue && media.PostId != id) ||
                    (!media.PostId.HasValue && (media.UploadedById != userId || media.CreatedAt < DateTime.UtcNow.AddDays(-1))))
                    throw new AppException("El adjunto no está disponible para esta publicación.");
                selected.Add(media);
            }
            else
                media.IsDeleted = true;
        }
        if (selected.Count(m => m.ContentType.StartsWith("image/")) > 4 || selected.Count(m => m.ContentType == "video/mp4") > 1)
            throw new AppException("Puedes adjuntar hasta 4 fotos y un video.");

        post.Title = request.Title.Trim();
        post.Content = request.Content.Trim();
        post.Kind = request.Kind;
        post.StartsAt = request.Kind == PostKinds.Competition ? request.StartsAt : null;
        post.Location = request.Kind == PostKinds.Competition ? request.Location?.Trim() : null;
        if (id.HasValue) post.UpdatedAt = DateTime.UtcNow;
        else db.Posts.Add(post);
        foreach (var media in selected)
        {
            media.Post = post;
            media.SortOrder = Array.IndexOf(request.MediaIds, media.Id);
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await GetAsync(post.Id, userId, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var post = await LockAsync(id, ct);
        post.IsDeleted = true;
        await db.MediaAssets.Where(m => m.PostId == id).ExecuteUpdateAsync(s => s.SetProperty(m => m.IsDeleted, true), ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task SetLikeAsync(int id, int userId, bool liked, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockAsync(id, ct);
        var existing = await db.PostLikes.FindAsync([id, userId], ct);
        if (liked && existing is null) db.PostLikes.Add(new PostLike { PostId = id, UserId = userId });
        if (!liked && existing is not null) db.PostLikes.Remove(existing);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private async Task<Post> LockAsync(int id, CancellationToken ct)
    {
        var post = await db.Posts.FromSql($"SELECT * FROM Posts WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id}")
            .FirstOrDefaultAsync(ct);
        return post is null || post.IsDeleted ? throw AppException.NotFound("La publicación no existe.") : post;
    }

    private static IQueryable<PostDto> Project(IQueryable<Post> query, int userId) => query.AsNoTracking()
        .Select(p => new PostDto(p.Id, p.Title, p.Content, p.Kind, p.CreatedAt, p.UpdatedAt,
            p.Author.FullName, p.StartsAt, p.Location, p.Likes.Count, p.Likes.Any(l => l.UserId == userId),
            p.Media.Where(m => m.IsReady && !m.IsDeleted).OrderBy(m => m.SortOrder)
                .Select(m => new MediaDto(m.Id, m.FileName, m.ContentType, m.Size)).ToList()));
}

/// <summary>Operaciones del tablón de novedades.</summary>
public interface IPostService
{
    Task<PageDto<PostDto>> ListAsync(int userId, int page, int pageSize, CancellationToken ct);
    Task<PostDto> GetAsync(int id, int userId, CancellationToken ct);
    Task<PostDto> SaveAsync(int? id, int userId, WritePostRequest request, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
    Task SetLikeAsync(int id, int userId, bool liked, CancellationToken ct);
}
