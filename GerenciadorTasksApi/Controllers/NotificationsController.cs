using GerenciadorTasks.Application.Dtos;
using GerenciadorTasks.Application.Services;
using GerenciadorTasks.Core.Exceptions;
using GerenciadorTasksApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GerenciadorTasksApi.Controllers;

/// <summary>
/// Endpoints REST para notificações do usuário logado. Rotas sob /api/notifications.
/// As notificações são geradas pelos serviços de Task/Reward em eventos chave.
/// </summary>
[ApiController]
[Authorize]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly NotificationService _service;

    public NotificationsController(NotificationService service) => _service = service;

    /// GET /api/notifications — lista as notificações do usuário (mais novas primeiro).
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationResponse>>> GetAll(CancellationToken ct)
    {
        var userId = User.GetUserId()
            ?? throw new DomainException("Usuário não autenticado.");
        return Ok(await _service.GetForUserAsync(userId, ct));
    }

    /// GET /api/notifications/unread-count — total de não-lidas (para o "sininho").
    [HttpGet("unread-count")]
    public async Task<ActionResult<object>> UnreadCount(CancellationToken ct)
    {
        var userId = User.GetUserId()
            ?? throw new DomainException("Usuário não autenticado.");
        var count = await _service.GetUnreadCountAsync(userId, ct);
        return Ok(new { count });
    }

    /// POST /api/notifications/{id}/read — marca uma notificação como lida (204).
    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId()
            ?? throw new DomainException("Usuário não autenticado.");
        await _service.MarkAsReadAsync(id, userId, ct);
        return NoContent();
    }
}
