using CalisApi.Common;
using CalisApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CalisApi.Services;

/// <summary>Paginación acotada compartida por los listados de comunidad.</summary>
public static class CommunityQueries
{
    public static async Task<PageDto<T>> PageAsync<T>(this IQueryable<T> query, int page, int pageSize, CancellationToken ct)
    {
        if (page is < 1 or > 100000 || pageSize is < 1 or > 50)
            throw new AppException("La página debe ser positiva y el tamaño entre 1 y 50.");
        return new(await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct),
            await query.CountAsync(ct), page, pageSize);
    }
}
