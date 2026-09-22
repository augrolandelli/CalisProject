using CalisApi.Common;
using CalisApi.Data;
using CalisApi.Dtos;
using CalisApi.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CalisApi.Services;

/// <summary>Reseñas verificadas por reserva previa y finalización, con moderación posterior.</summary>
public class ReviewService(CalisDbContext db, IValidator<WriteReviewRequest> validator) : IReviewService
{
    public async Task<ReviewPageDto> ListAsync(int? sessionId, bool moderation, int page, int pageSize, CancellationToken ct)
    {
        var query = db.SessionReviews.AsNoTracking().Where(r => moderation || !r.IsHidden);
        if (sessionId.HasValue) query = query.Where(r => r.SessionId == sessionId);
        var visible = query.Where(r => !r.IsHidden);
        var average = await visible.Select(r => (double?)r.Rating).AverageAsync(ct);
        var count = await visible.CountAsync(ct);
        var items = await query.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .Select(ToDto).PageAsync(page, pageSize, ct);
        return new(items, average.HasValue ? Math.Round(average.Value, 1) : null, count);
    }

    public async Task<ReviewEligibilityDto> EligibilityAsync(int sessionId, int userId, CancellationToken ct)
    {
        var session = await db.Sessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sessionId, ct)
            ?? throw AppException.NotFound("La clase no existe.");
        var mine = await db.SessionReviews.Where(r => r.SessionId == sessionId && r.UserId == userId).Select(ToDto).FirstOrDefaultAsync(ct);
        var reserved = await db.UserSessions.AnyAsync(r => r.SessionId == sessionId && r.UserId == userId && r.CreatedAt < session.Date, ct);
        var reason = !reserved ? "Necesitas una reserva previa al inicio de esta clase."
            : session.Date.AddMinutes(session.DurationMinutes) > DateTime.UtcNow ? "Podrás reseñar cuando termine la clase."
            : mine?.IsHidden == true ? "Esta reseña fue retirada por moderación." : null;
        return new(reason is null, reason, mine);
    }

    public async Task<ReviewDto> SaveAsync(int sessionId, int userId, WriteReviewRequest request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var session = await db.Sessions.FromSql($"SELECT * FROM Sessions WITH (UPDLOCK, HOLDLOCK) WHERE Id = {sessionId}")
            .FirstOrDefaultAsync(ct) ?? throw AppException.NotFound("La clase no existe.");
        var eligibility = await EligibilityAsync(sessionId, userId, ct);
        if (!eligibility.CanReview) throw new AppException(eligibility.Reason!);
        var review = await db.SessionReviews.FirstOrDefaultAsync(r => r.SessionId == sessionId && r.UserId == userId, ct);
        if (review is null)
        {
            review = new SessionReview
            {
                UserId = userId, SessionId = sessionId, SessionTitle = session.Title, SessionDate = session.Date,
            };
            db.SessionReviews.Add(review);
        }
        else review.UpdatedAt = DateTime.UtcNow;
        review.Rating = request.Rating;
        review.Content = string.IsNullOrWhiteSpace(request.Content) ? null : request.Content.Trim();
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await db.SessionReviews.Where(r => r.Id == review.Id).Select(ToDto).SingleAsync(ct);
    }

    public async Task DeleteOwnAsync(int id, int userId, CancellationToken ct)
    {
        var review = await db.SessionReviews.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw AppException.NotFound("La reseña no existe.");
        if (review.UserId != userId) throw AppException.Forbidden("Solo puedes eliminar tu propia reseña.");
        // Una reseña moderada conserva su identidad para impedir recrearla y evadir la moderación.
        if (review.IsHidden)
            await db.SessionReviews.Where(r => r.Id == id).ExecuteUpdateAsync(s => s.SetProperty(r => r.Content, (string?)null), ct);
        else
            await db.SessionReviews.Where(r => r.Id == id && !r.IsHidden).ExecuteDeleteAsync(ct);
    }

    public async Task ModerateAsync(int id, bool hidden, CancellationToken ct)
    {
        if (await db.SessionReviews.Where(r => r.Id == id).ExecuteUpdateAsync(s => s.SetProperty(r => r.IsHidden, hidden), ct) == 0)
            throw AppException.NotFound("La reseña no existe.");
    }

    private static System.Linq.Expressions.Expression<Func<SessionReview, ReviewDto>> ToDto => r =>
        new ReviewDto(r.Id, r.UserId, r.User.FullName, r.SessionId, r.SessionTitle, r.SessionDate,
            r.Rating, r.Content, r.CreatedAt, r.IsHidden);
}

public interface IReviewService
{
    Task<ReviewPageDto> ListAsync(int? sessionId, bool moderation, int page, int pageSize, CancellationToken ct);
    Task<ReviewEligibilityDto> EligibilityAsync(int sessionId, int userId, CancellationToken ct);
    Task<ReviewDto> SaveAsync(int sessionId, int userId, WriteReviewRequest request, CancellationToken ct);
    Task DeleteOwnAsync(int id, int userId, CancellationToken ct);
    Task ModerateAsync(int id, bool hidden, CancellationToken ct);
}
