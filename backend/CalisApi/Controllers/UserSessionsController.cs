using System.Security.Claims;
using CalisApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CalisApi.Controllers;

/// <summary>
/// Inscripciones y lista de espera de clases.
/// Requiere rol Clover o Admin (modelo freemium — spec §2).
/// </summary>
[ApiController]
[Route("api/usersession")]
[Authorize(Policy = "CloverOrAdmin")]
public class UserSessionsController(IUserSessionService userSessionService) : ControllerBase
{
    /// <summary>Inscribe al usuario autenticado en la clase (transaccional, anti-overbooking).</summary>
    [HttpPost("{sessionId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Enroll(int sessionId, CancellationToken ct)
    {
        await userSessionService.EnrollAsync(sessionId, GetUserId(), ct);
        return Ok(new { message = "Inscripción confirmada." });
    }

    /// <summary>Cancela la inscripción del usuario autenticado. Promueve la waitlist si hay.</summary>
    [HttpDelete("{sessionId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Unenroll(int sessionId, CancellationToken ct)
    {
        await userSessionService.UnenrollAsync(sessionId, GetUserId(), ct);
        return NoContent();
    }

    /// <summary>Une al usuario autenticado a la lista de espera (solo si la clase está llena).</summary>
    [HttpPost("{sessionId:int}/waitlist")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> JoinWaitlist(int sessionId, CancellationToken ct)
    {
        await userSessionService.JoinWaitlistAsync(sessionId, GetUserId(), ct);
        return Ok(new { message = "Te agregamos a la lista de espera." });
    }

    /// <summary>Saca al usuario autenticado de la lista de espera.</summary>
    [HttpDelete("{sessionId:int}/waitlist")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LeaveWaitlist(int sessionId, CancellationToken ct)
    {
        await userSessionService.LeaveWaitlistAsync(sessionId, GetUserId(), ct);
        return NoContent();
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
