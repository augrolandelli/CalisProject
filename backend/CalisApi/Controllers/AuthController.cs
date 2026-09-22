using CalisApi.Common;
using CalisApi.Dtos;
using CalisApi.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CalisApi.Controllers;

/// <summary>
/// Autenticación: registro, login, renovación y revocación de tokens.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(
    IAuthService authService,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    IValidator<RefreshRequest> refreshValidator) : ControllerBase
{
    /// <summary>Registra un usuario nuevo (rol inicial Guerrero) y devuelve tokens.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        await ValidateAsync(registerValidator, request, ct);
        return Ok(await authService.RegisterAsync(request, ct));
    }

    /// <summary>Autentica un usuario y devuelve tokens.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        await ValidateAsync(loginValidator, request, ct);
        return Ok(await authService.LoginAsync(request, ct));
    }

    /// <summary>Rota el refresh token y devuelve un nuevo par de tokens.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken ct)
    {
        await ValidateAsync(refreshValidator, request, ct);
        return Ok(await authService.RefreshAsync(request, ct));
    }

    /// <summary>Revoca el refresh token (cierre de sesión).</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            await authService.LogoutAsync(request.RefreshToken, ct);
        }
        return NoContent();
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T model, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(model, ct);
        if (!result.IsValid)
        {
            var message = string.Join(" ", result.Errors.Select(e => e.ErrorMessage));
            throw new AppException(message, StatusCodes.Status400BadRequest, "validation_error");
        }
    }
}
