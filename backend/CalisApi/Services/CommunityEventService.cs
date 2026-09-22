using CalisApi.Common;
using CalisApi.Data;
using CalisApi.Dtos;
using CalisApi.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CalisApi.Services;

/// <summary>Eventos e inscripciones: todas las mutaciones bloquean la misma fila para proteger cupos y fechas.</summary>
public class CommunityEventService(CalisDbContext db, IValidator<WriteEventRequest> validator) : ICommunityEventService
{
    public Task<PageDto<EventDto>> ListAsync(int userId, bool history, int page, int pageSize, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var query = db.CommunityEvents.AsNoTracking();
        var ordered = history ? query.Where(e => e.EndsAt <= now || e.IsCancelled).OrderByDescending(e => e.StartsAt).ThenByDescending(e => e.Id)
            : query.Where(e => e.EndsAt > now && !e.IsCancelled).OrderBy(e => e.StartsAt).ThenBy(e => e.Id);
        return Project(ordered, userId).PageAsync(page, pageSize, ct);
    }

    public async Task<EventDto> GetAsync(int id, int userId, CancellationToken ct) =>
        await Project(db.CommunityEvents.Where(e => e.Id == id), userId).FirstOrDefaultAsync(ct) ?? throw AppException.NotFound("El evento no existe.");

    public async Task<EventDto> SaveAsync(int? id, int userId, WriteEventRequest request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var item = id.HasValue ? await LockAsync(id.Value, ct) : new CommunityEvent
        {
            Title = request.Title.Trim(), Description = request.Description.Trim(), Location = request.Location.Trim(),
        };
        if (id.HasValue)
        {
            EnsureOpen(item);
            var count = await db.EventRegistrations.CountAsync(r => r.EventId == id, ct);
            if (request.Capacity < count) throw new AppException("El cupo no puede ser menor que la cantidad de inscritos.");
        }
        else db.CommunityEvents.Add(item);
        item.Title = request.Title.Trim();
        item.Description = request.Description.Trim();
        item.Location = request.Location.Trim();
        item.StartsAt = request.StartsAt;
        item.EndsAt = request.EndsAt;
        item.Capacity = request.Capacity;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await GetAsync(item.Id, userId, ct);
    }

    public async Task SetRegistrationAsync(int id, int userId, bool register, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var item = await LockAsync(id, ct);
        EnsureOpen(item);
        var existing = await db.EventRegistrations.FindAsync([id, userId], ct);
        if (register && existing is null)
        {
            if (item.Capacity.HasValue && await db.EventRegistrations.CountAsync(r => r.EventId == id, ct) >= item.Capacity)
                throw AppException.Conflict("El evento está completo.");
            db.EventRegistrations.Add(new EventRegistration { EventId = id, UserId = userId });
        }
        if (!register && existing is not null) db.EventRegistrations.Remove(existing);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task CancelAsync(int id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var item = await LockAsync(id, ct);
        if (!item.IsCancelled)
        {
            EnsureOpen(item);
            item.IsCancelled = true;
            await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
    }

    public async Task<PageDto<EventParticipantDto>> ParticipantsAsync(int id, int page, int pageSize, CancellationToken ct)
    {
        if (!await db.CommunityEvents.AnyAsync(e => e.Id == id, ct)) throw AppException.NotFound("El evento no existe.");
        return await db.EventRegistrations.AsNoTracking().Where(r => r.EventId == id)
            .OrderBy(r => r.CreatedAt).ThenBy(r => r.UserId)
            .Select(r => new EventParticipantDto(r.UserId, r.User.FullName, r.CreatedAt)).PageAsync(page, pageSize, ct);
    }

    private static void EnsureOpen(CommunityEvent item)
    {
        if (item.IsCancelled) throw new AppException("El evento fue cancelado.");
        if (item.StartsAt <= DateTime.UtcNow) throw new AppException("El evento ya comenzó; las inscripciones y cambios están cerrados.");
    }

    private async Task<CommunityEvent> LockAsync(int id, CancellationToken ct) =>
        await db.CommunityEvents.FromSql($"SELECT * FROM CommunityEvents WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id}")
            .FirstOrDefaultAsync(ct) ?? throw AppException.NotFound("El evento no existe.");

    private static IQueryable<EventDto> Project(IQueryable<CommunityEvent> query, int userId) => query.AsNoTracking()
        .Select(e => new EventDto(e.Id, e.Title, e.Description, e.Location, e.StartsAt, e.EndsAt, e.Capacity,
            e.Registrations.Count, e.Registrations.Any(r => r.UserId == userId), e.IsCancelled));
}

public interface ICommunityEventService
{
    Task<PageDto<EventDto>> ListAsync(int userId, bool history, int page, int pageSize, CancellationToken ct);
    Task<EventDto> GetAsync(int id, int userId, CancellationToken ct);
    Task<EventDto> SaveAsync(int? id, int userId, WriteEventRequest request, CancellationToken ct);
    Task SetRegistrationAsync(int id, int userId, bool register, CancellationToken ct);
    Task CancelAsync(int id, CancellationToken ct);
    Task<PageDto<EventParticipantDto>> ParticipantsAsync(int id, int page, int pageSize, CancellationToken ct);
}
