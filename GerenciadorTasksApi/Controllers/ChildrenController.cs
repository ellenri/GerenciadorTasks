using GerenciadorTasks.Application.Dtos;
using GerenciadorTasks.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GerenciadorTasksApi.Controllers;

/// <summary>Endpoints REST para crianças. Rotas sob /api/children.</summary>
[ApiController]
[Authorize]
[Route("api/children")]
public class ChildrenController : ControllerBase
{
    private readonly ChildService _service;

    public ChildrenController(ChildService service) => _service = service;

    /// GET /api/children — lista todas as crianças.
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ChildResponse>>> GetAll(CancellationToken ct)
        => Ok(await _service.GetAllAsync(ct));

    /// GET /api/children/{id} — detalhe de uma criança (inclui pontos).
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ChildResponse>> GetById(Guid id, CancellationToken ct)
    {
        var child = await _service.GetByIdAsync(id, ct);
        return child is null ? NotFound() : Ok(child);
    }

    /// POST /api/children — cadastra uma nova criança (201 Created + Location).
    [HttpPost]
    public async Task<ActionResult<ChildResponse>> Create(
        [FromBody] CreateChildRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
