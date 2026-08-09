using GerenciadorTasks.Application.Dtos;
using GerenciadorTasks.Application.Services;
using GerenciadorTasks.Core.Exceptions;
using GerenciadorTasksApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GerenciadorTasksApi.Controllers;

/// <summary>
/// Endpoints REST para recompensas. Rotas sob /api/rewards. Exige login ([Authorize]).
/// </summary>
[ApiController]
[Authorize]
[Route("api/rewards")]
public class RewardsController : ControllerBase
{
    private readonly RewardService _service;

    public RewardsController(RewardService service) => _service = service;

    /// GET /api/rewards — lista adaptada ao papel do usuário logado.
    /// Parent: recompensas que criou. Child: recompensas do seu responsável (vitrine).
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RewardResponse>>> GetAll(CancellationToken ct)
    {
        var userId = User.GetUserId()!.Value;
        var rewards = User.IsChild()
            ? await _service.GetForChildAsync(userId, ct)
            : await _service.GetForParentAsync(userId, ct);
        return Ok(rewards);
    }

    /// POST /api/rewards — cria uma recompensa (201 Created). Exclusivo do responsável.
    [HttpPost]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<RewardResponse>> Create(
        [FromBody] CreateRewardRequest request, CancellationToken ct)
    {
        var createdById = User.GetUserId()
            ?? throw new DomainException("Usuário não autenticado.");
        var created = await _service.CreateAsync(request, createdById, ct);
        return Created($"/api/rewards/{created.Id}", created);
    }

    /// POST /api/rewards/{id}/redeem — resgata a recompensa (desconta pontos).
    /// O pai resgata em nome das suas crianças; a criança só para si.
    [HttpPost("{id:guid}/redeem")]
    public async Task<ActionResult<RewardResponse>> Redeem(
        Guid id, [FromBody] RedeemRewardRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId() ?? throw new DomainException("Usuário não autenticado.");
        var role = User.GetRole() ?? throw new DomainException("Usuário sem papel definido.");
        return Ok(await _service.RedeemAsync(id, request.ChildId, userId, role, ct));
    }
}
