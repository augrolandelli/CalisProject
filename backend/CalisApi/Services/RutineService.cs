using CalisApi.Common;
using CalisApi.Data;
using CalisApi.Dtos;
using CalisApi.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CalisApi.Services;

/// <summary>Rutinas: lectura ordenada (calentamiento primero) y gestión Admin (Fase 5).</summary>
public class RutineService(CalisDbContext db, IValidator<WriteRutineRequest> validator) : IRutineService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<RutineSummaryDto>> GetAllAsync(
        int? categoryId, string? searchTerm, CancellationToken ct = default)
    {
        var query = db.Rutines.AsNoTracking().Include(r => r.Category).AsQueryable();

        if (categoryId is not null)
        {
            query = query.Where(r => r.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(r =>
                r.Title.ToLower().Contains(term) || r.Description.ToLower().Contains(term));
        }

        return await query
            .OrderBy(r => r.Title)
            .Select(r => new RutineSummaryDto(
                r.Id, r.Title, r.Description, r.Duration, r.Difficulty, r.Category.Name))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<RutineDetailDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var rutine = await db.Rutines
            .AsNoTracking()
            .Include(r => r.Exercises)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw AppException.NotFound("La rutina no existe.");

        // Regla de negocio: calentamiento primero, luego principales; dentro de cada
        // grupo se respeta el campo Order.
        var exercises = rutine.Exercises
            .OrderBy(e => e.Tipo == ExerciseTypes.Calentamiento ? 0 : 1)
            .ThenBy(e => e.Order)
            .Select(e => new RutineExerciseDto(
                e.Exercise, e.Tipo, e.Reps, e.Series, e.Descanso, e.Obs, e.VideoId))
            .ToList();

        return new RutineDetailDto(
            rutine.Id, rutine.Title, rutine.Description, rutine.Duration,
            rutine.Difficulty, rutine.CategoryId, exercises);
    }

    /// <inheritdoc />
    public async Task<RutineDetailDto> CreateAsync(WriteRutineRequest request, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        await ValidateReferencesAsync(request, ct);

        var rutine = new Rutine
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Duration = request.Duration.Trim(),
            Difficulty = request.Difficulty.Trim().ToLowerInvariant(),
            CategoryId = request.CategoryId,
        };
        AddExercises(rutine, request.Exercises);

        db.Rutines.Add(rutine);
        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(rutine.Id, ct);
    }

    /// <inheritdoc />
    public async Task<RutineDetailDto> UpdateAsync(int id, WriteRutineRequest request, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        await ValidateReferencesAsync(request, ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var rutine = await db.Rutines
            .Include(r => r.Exercises)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw AppException.NotFound("La rutina no existe.");

        rutine.Title = request.Title.Trim();
        rutine.Description = request.Description.Trim();
        rutine.Duration = request.Duration.Trim();
        rutine.Difficulty = request.Difficulty.Trim().ToLowerInvariant();
        rutine.CategoryId = request.CategoryId;

        // Reemplazo simple de la lista: los ejercicios son datos embebidos de la rutina.
        db.RutineExercises.RemoveRange(rutine.Exercises);
        AddExercises(rutine, request.Exercises);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await GetByIdAsync(rutine.Id, ct);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var rutine = await db.Rutines.FindAsync([id], ct)
            ?? throw AppException.NotFound("La rutina no existe.");
        db.Rutines.Remove(rutine);
        await db.SaveChangesAsync(ct);
    }

    private async Task ValidateReferencesAsync(WriteRutineRequest request, CancellationToken ct)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId, ct))
            throw AppException.NotFound("La categoría no existe.");

        var videoIds = request.Exercises.Where(e => e.VideoId is not null).Select(e => e.VideoId!.Value).Distinct().ToArray();
        if (videoIds.Length > 0
            && await db.Videos.CountAsync(v => videoIds.Contains(v.Id), ct) != videoIds.Length)
            throw AppException.NotFound("Uno o más videos de referencia no existen.");
    }

    /// <summary>Agrega ejercicios normalizando el orden: calentamiento primero, luego principales.</summary>
    private static void AddExercises(Rutine rutine, WriteRutineExercise[] exercises)
    {
        var ordered = exercises
            .Select((exercise, index) => (exercise, index))
            .OrderBy(x => x.exercise.Tipo == ExerciseTypes.Calentamiento ? 0 : 1)
            .ThenBy(x => x.index);

        var order = 1;
        foreach (var (exercise, _) in ordered)
        {
            rutine.Exercises.Add(new RutineExercise
            {
                Exercise = exercise.Exercise.Trim(),
                Tipo = exercise.Tipo,
                Reps = exercise.Reps,
                Series = exercise.Series,
                Descanso = exercise.Descanso.Trim(),
                Obs = exercise.Obs.Trim(),
                VideoId = exercise.VideoId,
                Order = order++,
            });
        }
    }
}

/// <summary>Contrato del servicio de rutinas.</summary>
public interface IRutineService
{
    Task<IReadOnlyList<RutineSummaryDto>> GetAllAsync(int? categoryId, string? searchTerm, CancellationToken ct = default);
    Task<RutineDetailDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<RutineDetailDto> CreateAsync(WriteRutineRequest request, CancellationToken ct = default);
    Task<RutineDetailDto> UpdateAsync(int id, WriteRutineRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
