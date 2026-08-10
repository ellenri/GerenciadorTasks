using GerenciadorTasks.Application.Dtos;
using GerenciadorTasks.Application.Services;
using GerenciadorTasks.Core.Exceptions;
using GerenciadorTasksApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace GerenciadorTasksApi.Controllers;

/// <summary>
/// Endpoints REST para missões (tarefas).
///
/// Fluxo de aprovação com comprovação:
///   criança -> POST /submit (envia foto)   -> PendingReview
///   pai    -> POST /approve                -> Completed (+ pontos)
///          ou POST /reject (com comentário) -> InProgress (criança refaz)
/// O pai também pode POST /skip (cancelar).
/// </summary>
[ApiController]
[Authorize]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private static readonly HashSet<string> AllowedExts =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
    private const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB

    private readonly TaskService _service;
    private readonly IWebHostEnvironment _env;

    public TasksController(TaskService service, IWebHostEnvironment env)
    {
        _service = service;
        _env = env;
    }

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

    /// POST /api/tasks — cria missão(ões). Para recorrência, gera várias instâncias.
    /// Exclusivo do responsável.
    [HttpPost]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> Create(
        [FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        var createdById = User.GetUserId()
            ?? throw new DomainException("Usuário não autenticado.");
        var created = await _service.CreateAsync(request, createdById, ct);
        return Ok(created);
    }

    /// POST /api/tasks/{id}/submit — a criança envia a foto de comprovação (multipart).
    /// A missão passa a aguardar aprovação do responsável (PendingReview).
    [HttpPost("{id:guid}/submit")]
    [RequestSizeLimit(MaxImageBytes)]
    public async Task<ActionResult<TaskResponse>> Submit(
        Guid id, [FromForm] IFormFile file, CancellationToken ct)
    {
        var imageUrl = await SaveSubmissionAsync(file, ct);
        return Ok(await _service.SubmitAsync(id, imageUrl, ct));
    }

    /// POST /api/tasks/{id}/approve — o responsável aprova a comprovação (conclui + pontos).
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<TaskResponse>> Approve(Guid id, CancellationToken ct)
        => Ok(await _service.ApproveAsync(id, ct));

    /// POST /api/tasks/{id}/reject — o responsável rejeita com comentário (criança refaz).
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<TaskResponse>> Reject(
        Guid id, [FromBody] RejectRequest request, CancellationToken ct)
        => Ok(await _service.RejectAsync(id, request.Comment, ct));

    /// POST /api/tasks/{id}/skip — cancela/abandona a missão. Exclusivo do responsável.
    [HttpPost("{id:guid}/skip")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<TaskResponse>> Skip(Guid id, CancellationToken ct)
        => Ok(await _service.SkipAsync(id, ct));

    /// <summary>Salva a imagem de comprovação em wwwroot/uploads e devolve a URL relativa.</summary>
    private async Task<string> SaveSubmissionAsync(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw new DomainException("Envie uma imagem de comprovação.");
        if (file.Length > MaxImageBytes)
            throw new DomainException("Imagem muito grande (máximo 5 MB).");

        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExts.Contains(ext))
            throw new DomainException("Formato de imagem não suportado (use jpg, png, gif ou webp).");

        var uploadsDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsDir);

        var name = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var path = Path.Combine(uploadsDir, name);

        await using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream, ct);

        return $"/uploads/{name}";
    }
}
