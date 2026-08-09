using GerenciadorTasks.Application.Dtos;
using GerenciadorTasks.Application.Services;
using GerenciadorTasks.Core.Exceptions;
using GerenciadorTasks.Infrastructure.Persistence;
using GerenciadorTasksApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GerenciadorTasksApi.Controllers;

/// <summary>
/// Endpoints REST para missões (tarefas).
/// Rotas sob /api/tasks. Os verbos HTTP mapeiam para os casos de uso.
/// </summary>
[ApiController]
[Authorize]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly TaskService _service;

    // Injeção de dependência: o controller recebe o serviço pronto.
    public TasksController(TaskService service) => _service = service;

    /// GET /api/tasks — lista adaptada ao papel do usuário logado.
    /// Parent: missões que criou. Child: missões atribuídas a ela.
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> GetAll(CancellationToken ct)
    {
        var userId = User.GetUserId()!.Value;
        var tasks = User.IsChild()
            ? await _service.GetForChildAsync(userId, ct)
            : await _service.GetForParentAsync(userId, ct);
        return Ok(tasks);
    }

    /// GET /api/tasks/{id} — detalhe de uma missão.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskResponse>> GetById(Guid id, CancellationToken ct)
    {
        var task = await _service.GetByIdAsync(id, ct);
        return task is null ? NotFound() : Ok(task);
    }

    /// POST /api/tasks — cria uma missão (201 Created + Location). Exclusivo do responsável.
    [HttpPost]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<TaskResponse>> Create(
        [FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        var createdById = User.GetUserId()
            ?? throw new DomainException("Usuário não autenticado.");
        var created = await _service.CreateAsync(request, createdById, ct);
        // CreatedAtAction devolve 201 + header Location apontando para GetById.
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// POST /api/tasks/{id}/complete — conclui a missão (gera pontos à criança).
    /// Sem try/catch: o DomainExceptionHandler global cuida dos erros de regra.
    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<TaskResponse>> Complete(Guid id, CancellationToken ct)
        => Ok(await _service.CompleteAsync(id, ct));
}
