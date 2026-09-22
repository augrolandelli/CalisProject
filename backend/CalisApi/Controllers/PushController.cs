using System.Security.Claims;
using CalisApi.Dtos;
using CalisApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CalisApi.Controllers;

/// <summary>Suscripciones Web Push del usuario autenticado.</summary>
[ApiController]
[Route("api/push")]
public class PushController(IPushService pushService) : ControllerBase
{
    /// <summary>Clave pública VAPID para suscribirse desde el navegador.</summary>
    [HttpGet("vapid-public-key")]
    [ProducesResponseType(typeof(VapidPublicKeyResponse), StatusCodes.Status200OK)]
    public IActionResult GetVapidPublicKey() =>
        Ok(new VapidPublicKeyResponse(pushService.GetPublicKey()));

    /// <summary>Registra (o actualiza) una suscripción push del usuario autenticado.</summary>
    [HttpPost("subscribe")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Subscribe(PushSubscriptionRequest request, CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await pushService.SubscribeAsync(userId, request, ct);
        return NoContent();
    }

    /// <summary>Elimina una suscripción push por endpoint.</summary>
    [HttpDelete("subscribe")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unsubscribe([FromBody] PushSubscriptionRequest request, CancellationToken ct)
    {
        await pushService.UnsubscribeAsync(request.Endpoint, ct);
        return NoContent();
    }
}
