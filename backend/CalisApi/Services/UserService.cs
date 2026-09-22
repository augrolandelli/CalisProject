using CalisApi.Common;
using CalisApi.Data;
using CalisApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CalisApi.Services;

/// <summary>Administración de usuarios (solo Admin).</summary>
public class UserService(CalisDbContext db) : IUserService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Dtos.AdminUserDto>> GetAllNonAdminAsync(
        string? search, string? role, CancellationToken ct = default)
    {
        var query = db.Users.AsNoTracking().Where(u => u.Role != Roles.Admin);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(role))
        {
            if (role is not (Roles.Guerrero or Roles.Clover))
                throw new AppException("Filtro de rol inválido (Guerrero o Clover).");
            query = query.Where(u => u.Role == role);
        }

        return await query
            .OrderBy(u => u.FullName)
            .Take(200)
            .Select(u => new Dtos.AdminUserDto(u.Id, u.FullName, u.Email, u.Role, u.State))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<Dtos.AdminUserDto> ChangeRoleAsync(int userId, string newRole, CancellationToken ct = default)
    {
        var validRoles = new[] { Roles.Guerrero, Roles.Clover, Roles.Admin };
        if (!validRoles.Contains(newRole))
        {
            throw new AppException($"Rol inválido. Válidos: {string.Join(", ", validRoles)}.");
        }

        var user = await db.Users.FindAsync([userId], ct)
            ?? throw AppException.NotFound("El usuario no existe.");

        user.Role = newRole;

        // Al dejar de ser Clover, sus refresh tokens quedan revocados (re-login limpio)
        await db.SaveChangesAsync(ct);

        return new Dtos.AdminUserDto(user.Id, user.FullName, user.Email, user.Role, user.State);
    }
}

/// <summary>Contrato del servicio de administración de usuarios.</summary>
public interface IUserService
{
    Task<IReadOnlyList<Dtos.AdminUserDto>> GetAllNonAdminAsync(string? search, string? role, CancellationToken ct = default);
    Task<Dtos.AdminUserDto> ChangeRoleAsync(int userId, string newRole, CancellationToken ct = default);
}
