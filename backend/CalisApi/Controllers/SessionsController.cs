using System.Security.Claims;
using CalisApi.Dtos;
using CalisApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CalisApi.Controllers;

/// <summary>Clases en vivo: listado por fecha, detalle y participantes.</summary>
[ApiController]
[Route("api/session")]
public class SessionsController(ISessionService sessionService) : ControllerBase
{
    /// <summary>Lista sesiones, opcionalmente filtradas por fecha (?datetime=YYYY-MM-DD).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] DateTime? datetime, CancellationToken ct) =>
        Ok(await sessionService.GetAllAsync(datetime, ct));

    /// <summary>Obtiene una sesión por id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct) =>
        Ok(await sessionService.GetByIdAsync(id, ct));

    /// <summary>Detalle con participantes y estado del usuario autenticado (si lo hay).</summary>
    [HttpGet("{id:int}/details")]
    [ProducesResponseType(typeof(SessionDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetails(int id, CancellationToken ct) =>
        Ok(await sessionService.GetDetailsAsync(id, GetCurrentUserId(), ct));

    /// <summary>Solo usuarios inscritos a una sesión.</summary>
    [HttpGet("{id:int}/users")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<EnrolledUserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(int id, CancellationToken ct) =>
        Ok((await sessionService.GetDetailsAsync(id, null, ct)).EnrolledUsers);

    /// <summary>Crea una clase (Admin).</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(CreateSessionRequest request, CancellationToken ct) =>
        Ok(await sessionService.CreateAsync(request, ct));

    /// <summary>Marca asistencia de inscritos a una clase finalizada (Admin).</summary>
    [HttpPut("{id:int}/attendance")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordAttendance(int id, RecordAttendanceRequest request, CancellationToken ct)
    {
        await sessionService.RecordAttendanceAsync(id, request, ct);
        return NoContent();
    }

    /// <summary>Edita una clase (Admin). Si cambia la fecha, avisa por push a los inscritos.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, CreateSessionRequest request, CancellationToken ct) =>
        Ok(await sessionService.UpdateAsync(id, request, ct));

    /// <summary>Cantidad de clases de un mes pasado (preview del borrado masivo, Admin).</summary>
    [HttpGet("purge-preview")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PurgePreview([FromQuery] int year, [FromQuery] int month, CancellationToken ct) =>
        Ok(new { count = await sessionService.CountByMonthAsync(year, month, ct) });

    /// <summary>Eliminación masiva de clases de un mes ya finalizado (Admin).</summary>
    [HttpDelete]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteByMonth([FromQuery] int year, [FromQuery] int month, CancellationToken ct) =>
        Ok(new { deleted = await sessionService.DeleteByMonthAsync(year, month, ct) });

    /// <summary>Elimina una clase (Admin).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await sessionService.DeleteAsync(id, ct);
        return NoContent();
    }

    private int? GetCurrentUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
