using CalisApi.Auth;
using CalisApi.Common;
using CalisApi.Data;
using CalisApi.Dtos;
using CalisApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CalisApi.Services;

/// <summary>
/// Lógica de autenticación: registro, login, rotación de refresh tokens y logout.
/// </summary>
public class AuthService(
    CalisDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IConfiguration configuration) : IAuthService
{
    /// <inheritdoc />
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var emailTaken = await db.Users.AnyAsync(u => u.Email == email, ct);
        if (emailTaken)
        {
            throw AppException.Conflict("El email ya está registrado.");
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Phone = request.Phone.Trim(),
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = Roles.Guerrero, // el rol inicial lo fija el servidor, nunca el cliente
            State = "Activo",
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return await CreateSessionAsync(user, ct);
    }

    /// <inheritdoc />
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Mensaje genérico para no revelar si el email existe (enumeración de usuarios)
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw AppException.Unauthorized("Email o contraseña incorrectos.");
        }

        if (user.State != "Activo")
        {
            throw AppException.Forbidden("La cuenta no está activa.");
        }

        return await CreateSessionAsync(user, ct);
    }

    /// <inheritdoc />
    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var storedToken = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, ct);

        if (storedToken is null || !storedToken.IsActive)
        {
            throw AppException.Unauthorized("Refresh token inválido o expirado.");
        }

        // Rotación: el token usado queda revocado y se reemplaza por uno nuevo
        var (accessToken, expiresAt) = tokenService.CreateAccessToken(storedToken.User);
        var newRefreshToken = CreateRefreshTokenEntity(storedToken.User);

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByToken = newRefreshToken.Token;

        db.RefreshTokens.Add(newRefreshToken);
        await db.SaveChangesAsync(ct);

        return new AuthResponse(accessToken, newRefreshToken.Token, expiresAt, ToDto(storedToken.User));
    }

    /// <inheritdoc />
    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var storedToken = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken, ct);

        if (storedToken is { IsActive: true })
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task<AuthResponse> CreateSessionAsync(User user, CancellationToken ct)
    {
        var (accessToken, expiresAt) = tokenService.CreateAccessToken(user);
        var refreshToken = CreateRefreshTokenEntity(user);

        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(ct);

        return new AuthResponse(accessToken, refreshToken.Token, expiresAt, ToDto(user));
    }

    private RefreshToken CreateRefreshTokenEntity(User user)
    {
        var refreshDays = configuration.GetValue("Jwt:RefreshTokenDays", 14);
        return new RefreshToken
        {
            Token = tokenService.CreateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDays),
            UserId = user.Id,
        };
    }

    private static UserDto ToDto(User user) =>
        new(user.Id, user.FullName, user.Phone, user.Email, user.Role, user.State, user.PhotoUrl);
}

/// <summary>Contrato del servicio de autenticación.</summary>
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
}
