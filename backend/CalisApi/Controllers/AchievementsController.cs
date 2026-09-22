using System.Security.Claims;
using CalisApi.Dtos;
using CalisApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CalisApi.Controllers;

/// <summary>Logros: catálogo, consultas y otorgamiento.</summary>
[ApiController]
[Route("api/achievement")]
public class AchievementsController(IAchievementService achievementService) : ControllerBase
{
    /// <summary>Catálogo completo de logros.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AchievementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await achievementService.GetAllAsync(ct));

    /// <summary>Ids de usuarios que obtuvieron un logro.</summary>
    [HttpGet("{id:int}/users")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserIds(int id, CancellationToken ct) =>
        Ok(await achievementService.GetUserIdsAsync(id, ct));

    /// <summary>Logros asociados a una clase (los que se pueden ganar en ella).</summary>
    [HttpGet("/api/session/{sessionId:int}/achievements")]
    [ProducesResponseType(typeof(IReadOnlyList<AchievementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForSession(int sessionId, CancellationToken ct) =>
        Ok(await achievementService.GetForSessionAsync(sessionId, ct));

    /// <summary>Logros obtenidos por un usuario.</summary>
    [HttpGet("/api/user/{userId:int}/achievements")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<UserAchievementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForUser(int userId, CancellationToken ct) =>
        Ok(await achievementService.GetForUserAsync(userId, ct));

    /// <summary>Catálogo Admin: incluye ocultos y cantidad de dueños.</summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(IReadOnlyList<AchievementAdminDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken ct) =>
        Ok(await achievementService.GetAllForAdminAsync(ct));

    /// <summary>Crea un logro (Admin).</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(AchievementDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(CreateAchievementRequest request, CancellationToken ct) =>
        Ok(await achievementService.CreateAsync(request, ct));

    /// <summary>Edita un logro (Admin).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(AchievementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, CreateAchievementRequest request, CancellationToken ct) =>
        Ok(await achievementService.UpdateAsync(id, request, ct));

    /// <summary>Elimina un logro (Admin): borrado físico si nadie lo ganó; si tiene dueños se oculta.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await achievementService.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Asocia un logro a una clase (Admin).</summary>
    [HttpPost("/api/session/{sessionId:int}/achievements/{achievementId:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssociateToSession(int sessionId, int achievementId, CancellationToken ct)
    {
        await achievementService.AssociateToSessionAsync(sessionId, achievementId, ct);
        return NoContent();
    }

    /// <summary>Otorga un logro a usuarios al finalizar una clase (Admin/Coach).</summary>
    [HttpPost("grant")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(GrantAchievementResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Grant(GrantAchievementRequest request, CancellationToken ct) =>
        Ok(await achievementService.GrantAsync(request, ct));
}
