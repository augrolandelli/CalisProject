using System.Security.Claims;
using CalisApi.Data;
using CalisApi.Dtos;
using CalisApi.Models;
using CalisApi.Services;
using CalisApi.Services.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CalisApi.Controllers;

/// <summary>Identidad autenticada compartida por los endpoints de comunidad.</summary>
public abstract class CommunityControllerBase : ControllerBase
{
    protected int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

/// <summary>Tablón exclusivo de Clover; edición reservada al Admin.</summary>
[ApiController, Route("api/post"), Authorize(Policy = "CloverOrAdmin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class PostsController(IPostService service) : CommunityControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageDto<PostDto>>> List(CancellationToken ct, int page = 1, int pageSize = 20) =>
        Ok(await service.ListAsync(UserId, page, pageSize, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PostDto>> Get(int id, CancellationToken ct) => Ok(await service.GetAsync(id, UserId, ct));

    [HttpPost, Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<PostDto>> Create(WritePostRequest request, CancellationToken ct) =>
        Ok(await service.SaveAsync(null, UserId, request, ct));

    [HttpPut("{id:int}"), Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<PostDto>> Edit(int id, WritePostRequest request, CancellationToken ct) =>
        Ok(await service.SaveAsync(id, UserId, request, ct));

    [HttpDelete("{id:int}"), Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    { await service.DeleteAsync(id, ct); return NoContent(); }

    [HttpPut("{id:int}/like")]
    public async Task<IActionResult> Like(int id, CancellationToken ct)
    { await service.SetLikeAsync(id, UserId, true, ct); return NoContent(); }

    [HttpDelete("{id:int}/like")]
    public async Task<IActionResult> Unlike(int id, CancellationToken ct)
    { await service.SetLikeAsync(id, UserId, false, ct); return NoContent(); }
}

/// <summary>Reseñas legibles para todo usuario registrado y escritura para miembros con reserva.</summary>
[ApiController, Route("api/review"), Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ReviewsController(IReviewService service) : CommunityControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ReviewPageDto>> List(CancellationToken ct, int? sessionId = null, int page = 1, int pageSize = 20) =>
        Ok(await service.ListAsync(sessionId, false, page, pageSize, ct));

    [HttpGet("moderation"), Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ReviewPageDto>> Moderation(CancellationToken ct, int page = 1, int pageSize = 20) =>
        Ok(await service.ListAsync(null, true, page, pageSize, ct));

    [HttpGet("session/{sessionId:int}/mine"), Authorize(Policy = "CloverOrAdmin")]
    public async Task<ActionResult<ReviewEligibilityDto>> Eligibility(int sessionId, CancellationToken ct) =>
        Ok(await service.EligibilityAsync(sessionId, UserId, ct));

    [HttpPut("session/{sessionId:int}"), Authorize(Policy = "CloverOrAdmin")]
    public async Task<ActionResult<ReviewDto>> Save(int sessionId, WriteReviewRequest request, CancellationToken ct) =>
        Ok(await service.SaveAsync(sessionId, UserId, request, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    { await service.DeleteOwnAsync(id, UserId, ct); return NoContent(); }

    [HttpPut("{id:int}/visibility"), Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Moderate(int id, ReviewVisibilityRequest request, CancellationToken ct)
    { await service.ModerateAsync(id, request.IsHidden, ct); return NoContent(); }
}

/// <summary>Eventos exclusivos e inscripciones.</summary>
[ApiController, Route("api/event"), Authorize(Policy = "CloverOrAdmin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class CommunityEventsController(ICommunityEventService service) : CommunityControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageDto<EventDto>>> List(CancellationToken ct, bool history = false, int page = 1, int pageSize = 20) =>
        Ok(await service.ListAsync(UserId, history, page, pageSize, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventDto>> Get(int id, CancellationToken ct) => Ok(await service.GetAsync(id, UserId, ct));

    [HttpPost, Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<EventDto>> Create(WriteEventRequest request, CancellationToken ct) =>
        Ok(await service.SaveAsync(null, UserId, request, ct));

    [HttpPut("{id:int}"), Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<EventDto>> Edit(int id, WriteEventRequest request, CancellationToken ct) =>
        Ok(await service.SaveAsync(id, UserId, request, ct));

    [HttpPost("{id:int}/cancel"), Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    { await service.CancelAsync(id, ct); return NoContent(); }

    [HttpPut("{id:int}/registration")]
    public async Task<IActionResult> Register(int id, CancellationToken ct)
    { await service.SetRegistrationAsync(id, UserId, true, ct); return NoContent(); }

    [HttpDelete("{id:int}/registration")]
    public async Task<IActionResult> Unregister(int id, CancellationToken ct)
    { await service.SetRegistrationAsync(id, UserId, false, ct); return NoContent(); }

    [HttpGet("{id:int}/participants"), Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<PageDto<EventParticipantDto>>> Participants(int id, CancellationToken ct, int page = 1, int pageSize = 20) =>
        Ok(await service.ParticipantsAsync(id, page, pageSize, ct));
}

/// <summary>Subidas Admin y acceso temporal a adjuntos exclusivos.</summary>
[ApiController, Route("api/media"), Authorize(Policy = "CloverOrAdmin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class MediaController(
    IMediaService service,
    LocalFileStorage local,
    CalisDbContext db) : CommunityControllerBase
{
    [HttpPost, Authorize(Policy = "AdminOnly"), EnableRateLimiting("media-upload")]
    public async Task<ActionResult<UploadDto>> Begin(BeginUploadRequest request, CancellationToken ct) =>
        Ok(await service.BeginAsync(UserId, request, ct));

    /// <summary>
    /// PUT de subida a staging del proveedor Local. Sin JWT: la URL firmada (HMAC + expiración,
    /// emitida solo a Admins) es la credencial, igual que una URL prefirmada de R2.
    /// </summary>
    [HttpPut("staging/{**key}")]
    [AllowAnonymous]
    [EnableRateLimiting("media-upload")]
    [RequestSizeLimit(105 * 1024 * 1024)]
    public async Task<IActionResult> UploadStaging(
        string key, [FromQuery] long expires, [FromQuery] string? signature, CancellationToken ct)
    {
        if (!key.StartsWith("staging/", StringComparison.Ordinal)
            || !local.IsConfigured
            || !local.Validate(key, expires, signature))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "La URL de subida es inválida o expiró.", code = "invalid_upload_url" });
        await local.WriteStreamAsync(key, Request.Body, ct);
        return NoContent();
    }

    /// <summary>Descarga firmada de un adjunto del proveedor Local (con soporte de rangos para video).</summary>
    [HttpGet("files/{**key}")]
    [AllowAnonymous]
    public async Task<IActionResult> Download(
        string key, [FromQuery] long expires, [FromQuery] string? signature, CancellationToken ct)
    {
        if (!local.IsConfigured || !local.Validate(key, expires, signature))
            return NotFound(new { message = "El archivo no está disponible.", code = "not_found" });
        var path = local.PathFor(key);
        if (!System.IO.File.Exists(path)) return NotFound(new { message = "El archivo no está disponible.", code = "not_found" });
        var contentType = await db.MediaAssets.AsNoTracking()
            .Where(m => m.ObjectKey == key)
            .Select(m => m.ContentType)
            .FirstOrDefaultAsync(ct) ?? "application/octet-stream";
        Response.Headers.CacheControl = "private, no-store";
        return PhysicalFile(path, contentType, enableRangeProcessing: true);
    }

    [HttpPost("{id:guid}/complete"), Authorize(Policy = "AdminOnly"), EnableRateLimiting("media-upload")]
    public async Task<ActionResult<MediaDto>> Complete(Guid id, CancellationToken ct) =>
        Ok(await service.CompleteAsync(id, UserId, ct));

    [HttpGet("{id:guid}/link")]
    public async Task<ActionResult<MediaLinkDto>> Link(Guid id, CancellationToken ct) =>
        Ok(await service.LinkAsync(id, UserId, User.IsInRole(Roles.Admin), ct));

    [HttpDelete("{id:guid}"), Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Abandon(Guid id, CancellationToken ct)
    { await service.AbandonAsync(id, UserId, ct); return NoContent(); }
}
