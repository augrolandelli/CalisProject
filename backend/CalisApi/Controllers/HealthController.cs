using Microsoft.AspNetCore.Mvc;

namespace CalisApi.Controllers;

/// <summary>
/// Endpoint de salud para monitoreo y verificacion de despliegue.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Verifica que la API este en linea.
    /// </summary>
    /// <returns>Estado actual de la API y timestamp UTC.</returns>
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "healthy",
        service = "CalisApi",
        timestamp = DateTime.UtcNow
    });
}
