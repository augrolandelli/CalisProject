using CalisApi.Dtos;
using CalisApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CalisApi.Controllers;

/// <summary>Gestión de usuarios — solo Admin.</summary>
[ApiController]
[Route("api/user")]
[Authorize(Policy = "AdminOnly")]
public class UsersController(IUserService userService) : ControllerBase
{
    /// <summary>Lista usuarios (excluye admins) con buscador y filtro por rol.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AdminUserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? role,
        CancellationToken ct) =>
        Ok(await userService.GetAllNonAdminAsync(search, role, ct));

    /// <summary>Cambia el rol de un usuario (Guerrero / Clover / Admin).</summary>
    [HttpPatch("{id:int}/role")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeRole(int id, ChangeRoleRequest request, CancellationToken ct) =>
        Ok(await userService.ChangeRoleAsync(id, request.Role, ct));
}
