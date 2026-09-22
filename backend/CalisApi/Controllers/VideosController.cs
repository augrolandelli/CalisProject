using System.Security.Claims;
using CalisApi.Dtos;
using CalisApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CalisApi.Controllers;

/// <summary>Videoteca de ejercicios. Lectura: cualquier rol. Gestión: Admin.</summary>
[ApiController]
[Route("api/video")]
[Authorize]
public class VideosController(IVideoService videoService) : ControllerBase
{
    /// <summary>Lista videos con filtros opcionales por categoría y término de búsqueda.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VideoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? categoryId,
        [FromQuery] string? searchTerm,
        CancellationToken ct) =>
        Ok(await videoService.GetAllAsync(categoryId, searchTerm, ct));

    /// <summary>Obtiene un video por id (con categoría anidada y URL de reproducción firmada).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(VideoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct) =>
        Ok(await videoService.GetByIdAsync(id, ct));

    /// <summary>Crea un video con archivo ya subido/validado (Admin).</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(VideoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(WriteVideoRequest request, CancellationToken ct) =>
        Ok(await videoService.CreateAsync(GetUserId(), request, ct));

    /// <summary>Edita metadatos y opcionalmente reemplaza el archivo (Admin).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(VideoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, WriteVideoRequest request, CancellationToken ct) =>
        Ok(await videoService.UpdateAsync(id, GetUserId(), request, ct));

    /// <summary>Elimina un video (Admin). Bloqueado si lo usa alguna rutina.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await videoService.DeleteAsync(id, ct);
        return NoContent();
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
