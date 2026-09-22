using CalisApi.Common;
using CalisApi.Data;
using CalisApi.Dtos;
using CalisApi.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CalisApi.Services;

/// <summary>Categorías: lectura pública y gestión Admin (Fase 5).</summary>
public class CategoryService(CalisDbContext db, IValidator<WriteCategoryRequest> validator) : ICategoryService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Description))
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<CategoryDto> GetByIdAsync(int id, CancellationToken ct = default) =>
        await db.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Description))
            .FirstOrDefaultAsync(ct)
        ?? throw AppException.NotFound("La categoría no existe.");

    /// <inheritdoc />
    public async Task<CategoryDto> CreateAsync(WriteCategoryRequest request, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        await EnsureUniqueNameAsync(request.Name, null, ct);

        var category = new Category { Name = request.Name.Trim(), Description = request.Description.Trim() };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return new CategoryDto(category.Id, category.Name, category.Description);
    }

    /// <inheritdoc />
    public async Task<CategoryDto> UpdateAsync(int id, WriteCategoryRequest request, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        var category = await db.Categories.FindAsync([id], ct)
            ?? throw AppException.NotFound("La categoría no existe.");
        await EnsureUniqueNameAsync(request.Name, id, ct);

        category.Name = request.Name.Trim();
        category.Description = request.Description.Trim();
        await db.SaveChangesAsync(ct);
        return new CategoryDto(category.Id, category.Name, category.Description);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var category = await db.Categories.FindAsync([id], ct)
            ?? throw AppException.NotFound("La categoría no existe.");

        var videos = await db.Videos.CountAsync(v => v.CategoryId == id, ct);
        var rutines = await db.Rutines.CountAsync(r => r.CategoryId == id, ct);
        if (videos > 0 || rutines > 0)
            throw new AppException($"No se puede eliminar: la categoría tiene {videos} video(s) y {rutines} rutina(s). Reasigna o elimina ese contenido primero.");

        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureUniqueNameAsync(string name, int? excludeId, CancellationToken ct)
    {
        var normalized = name.Trim();
        var exists = await db.Categories.AnyAsync(c =>
            c.Name.ToLower() == normalized.ToLower() && (excludeId == null || c.Id != excludeId), ct);
        if (exists)
            throw AppException.Conflict($"Ya existe una categoría llamada \"{normalized}\".");
    }
}

/// <summary>Contrato del servicio de categorías.</summary>
public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default);
    Task<CategoryDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<CategoryDto> CreateAsync(WriteCategoryRequest request, CancellationToken ct = default);
    Task<CategoryDto> UpdateAsync(int id, WriteCategoryRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
