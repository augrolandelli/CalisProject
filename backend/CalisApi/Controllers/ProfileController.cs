using System.Security.Claims;
using CalisApi.Dtos;
using CalisApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CalisApi.Controllers;

/// <summary>Perfil del usuario autenticado: datos, edición, logros y estadísticas.</summary>
[ApiController]
[Route("api/me")]
[Authorize]
public class ProfileController(IProfileService profileService, IAchievementService achievementService) : ControllerBase
{
    /// <summary>Datos del perfil propio.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        Ok(await profileService.GetAsync(GetUserId(), ct));

    /// <summary>Edita nombre, teléfono y foto del perfil propio.</summary>
    [HttpPatch]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(UpdateProfileRequest request, CancellationToken ct) =>
        Ok(await profileService.UpdateAsync(GetUserId(), request, ct));

    /// <summary>Estadísticas de progreso (clases, logros).</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(UserStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken ct) =>
        Ok(await profileService.GetStatsAsync(GetUserId(), ct));

    /// <summary>Logros obtenidos por el usuario autenticado.</summary>
    [HttpGet("achievements")]
    [ProducesResponseType(typeof(IReadOnlyList<UserAchievementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAchievements(CancellationToken ct) =>
        Ok(await achievementService.GetForUserAsync(GetUserId(), ct));

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
