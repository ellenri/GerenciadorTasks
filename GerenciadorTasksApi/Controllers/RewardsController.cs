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

    /// GET /api/rewards — lista todas as recompensas.
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RewardResponse>>> GetAll(CancellationToken ct)
        => Ok(await _service.GetAllAsync(ct));

    /// POST /api/rewards — cria uma recompensa (201 Created).
    [HttpPost]
    public async Task<ActionResult<RewardResponse>> Create(
        [FromBody] CreateRewardRequest request, CancellationToken ct)
    {
        var createdById = User.GetUserId()
            ?? throw new DomainException("Usuário não autenticado.");
        var created = await _service.CreateAsync(request, createdById, ct);
        return Created($"/api/rewards/{created.Id}", created);
    }

    /// POST /api/rewards/{id}/redeem — resgata a recompensa para uma criança (desconta pontos).
    [HttpPost("{id:guid}/redeem")]
    public async Task<ActionResult<RewardResponse>> Redeem(
        Guid id, [FromBody] RedeemRewardRequest request, CancellationToken ct)
        => Ok(await _service.RedeemAsync(id, request.ChildId, ct));
}
