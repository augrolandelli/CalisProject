using CalisApi.Dtos;
using CalisApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CalisApi.Controllers;

/// <summary>Rutinas de entrenamiento. Requiere autenticación (cualquier rol).</summary>
[ApiController]
[Route("api/rutine")]
[Authorize]
public class RutinesController(IRutineService rutineService) : ControllerBase
{
    /// <summary>Lista resumida de rutinas con filtros opcionales.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RutineSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? categoryId,
        [FromQuery] string? searchTerm,
        CancellationToken ct) =>
        Ok(await rutineService.GetAllAsync(categoryId, searchTerm, ct));

    /// <summary>Detalle completo de una rutina (calentamiento primero, luego principales).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RutineDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct) =>
        Ok(await rutineService.GetByIdAsync(id, ct));

    /// <summary>Crea una rutina con sus ejercicios (Admin). El servidor ordena: calentamiento primero.</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(RutineDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(WriteRutineRequest request, CancellationToken ct) =>
        Ok(await rutineService.CreateAsync(request, ct));

    /// <summary>Edita una rutina y reemplaza sus ejercicios (Admin).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(RutineDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, WriteRutineRequest request, CancellationToken ct) =>
        Ok(await rutineService.UpdateAsync(id, request, ct));

    /// <summary>Elimina una rutina (Admin).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await rutineService.DeleteAsync(id, ct);
        return NoContent();
    }
}
