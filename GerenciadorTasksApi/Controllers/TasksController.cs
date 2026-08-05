using GerenciadorTasks.Application.Dtos;
using GerenciadorTasks.Application.Services;
using GerenciadorTasks.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace GerenciadorTasksApi.Controllers;

/// <summary>
/// Endpoints REST para missões (tarefas).
/// Rotas sob /api/tasks. Os verbos HTTP mapeiam para os casos de uso.
/// </summary>
[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly TaskService _service;

    // Injeção de dependência: o controller recebe o serviço pronto.
    public TasksController(TaskService service) => _service = service;

    /// GET /api/tasks — lista todas as missões.
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> GetAll(CancellationToken ct)
        => Ok(await _service.GetAllAsync(ct));

    /// GET /api/tasks/{id} — detalhe de uma missão.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskResponse>> GetById(Guid id, CancellationToken ct)
    {
        var task = await _service.GetByIdAsync(id, ct);
        return task is null ? NotFound() : Ok(task);
    }

    /// POST /api/tasks — cria uma missão (201 Created + Location).
    [HttpPost]
    public async Task<ActionResult<TaskResponse>> Create(
        [FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, SeedData.DefaultParentId, ct);
        // CreatedAtAction devolve 201 + header Location apontando para GetById.
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// POST /api/tasks/{id}/complete — conclui a missão (gera pontos à criança).
    /// Sem try/catch: o DomainExceptionHandler global cuida dos erros de regra.
    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<TaskResponse>> Complete(Guid id, CancellationToken ct)
        => Ok(await _service.CompleteAsync(id, ct));
}
