using GerenciadorTasks.Application.Dtos;
using GerenciadorTasks.Application.Services;
using GerenciadorTasks.Core.Exceptions;
using GerenciadorTasksApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GerenciadorTasksApi.Controllers;

/// <summary>
/// Endpoints REST para crianças. Rotas sob /api/children.
///
/// O cadastro (POST) é exclusivo do responsável (Role=Parent). A consulta é
/// adaptada ao papel: o pai vê as crianças que cadastrou; a criança vê só a si.
/// </summary>
[ApiController]
[Authorize]
[Route("api/children")]
public class ChildrenController : ControllerBase
{
    private readonly ChildService _service;

    public ChildrenController(ChildService service) => _service = service;

    /// GET /api/children — lista adaptada ao papel do usuário logado.
    /// Parent: as crianças que cadastrou. Child: apenas a si mesma.
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ChildResponse>>> GetAll(CancellationToken ct)
    {
        if (User.IsParent())
        {
            var parentUserId = User.GetUserId()!.Value;
            return Ok(await _service.ListByParentAsync(parentUserId, ct));
        }

        // Criança logada: devolve uma lista contendo só o próprio perfil.
        var me = await _service.GetByUserIdAsync(User.GetUserId()!.Value, ct);
        return Ok(me is null ? Array.Empty<ChildResponse>() : new[] { me });
    }

    /// GET /api/children/me — perfil da própria criança logada (404 se não for criança).
    [HttpGet("me")]
    public async Task<ActionResult<ChildResponse>> GetMe(CancellationToken ct)
    {
        if (!User.IsChild())
            return NotFound(new { message = "Apenas contas de criança têm perfil de criança." });

        var me = await _service.GetByUserIdAsync(User.GetUserId()!.Value, ct);
        return me is null ? NotFound() : Ok(me);
    }

    /// GET /api/children/{id} — detalhe de uma criança (inclui pontos).
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ChildResponse>> GetById(Guid id, CancellationToken ct)
    {
        var child = await _service.GetByIdAsync(id, ct);
        return child is null ? NotFound() : Ok(child);
    }

    /// GET /api/children/{id}/edit — dados para edição (perfil + e-mail). Exclusivo do responsável dono.
    [HttpGet("{id:guid}/edit")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<ChildResponse>> GetForEdit(Guid id, CancellationToken ct)
    {
        var parentUserId = User.GetUserId()
            ?? throw new DomainException("Responsável não autenticado.");
        var child = await _service.GetForEditAsync(id, parentUserId, ct);
        return child is null ? NotFound() : Ok(child);
    }

    /// POST /api/children — cadastra uma criança (com login próprio). Exclusivo do responsável.
    [HttpPost]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<ChildResponse>> Create(
        [FromBody] CreateChildRequest request, CancellationToken ct)
    {
        var parentUserId = User.GetUserId()
            ?? throw new DomainException("Responsável não autenticado.");
        var created = await _service.CreateAsync(request, parentUserId, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// PUT /api/children/{id} — edita uma criança (perfil + e-mail/senha). Exclusivo do responsável.
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<ChildResponse>> Update(
        Guid id, [FromBody] UpdateChildRequest request, CancellationToken ct)
    {
        var parentUserId = User.GetUserId()
            ?? throw new DomainException("Responsável não autenticado.");
        var updated = await _service.UpdateAsync(id, request, parentUserId, ct);
        return updated is null ? NotFound() : Ok(updated);
    }
}
