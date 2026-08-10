using GerenciadorTasks.Application.Abstractions;
using GerenciadorTasks.Application.Dtos;
using GerenciadorTasks.Application.Mapping;
using GerenciadorTasks.Core.Entities;
using GerenciadorTasks.Core.Enums;
using GerenciadorTasks.Core.Exceptions;

namespace GerenciadorTasks.Application.Services;

/// <summary>
/// Casos de uso relacionados a missões (TaskItem).
///
/// O serviço ORQUESTRA entre agregados e repositórios — ele não contém
/// regras de negócio (elas vivem nas entidades). É a "camada de aplicação".
/// </summary>
public class TaskService
{
    private readonly ITaskRepository _tasks;
    private readonly IChildRepository _children;
    private readonly INotificationRepository _notifications;
    private readonly IUnitOfWork _unitOfWork;

    public TaskService(
        ITaskRepository tasks,
        IChildRepository children,
        INotificationRepository notifications,
        IUnitOfWork unitOfWork)
    {
        _tasks = tasks;
        _children = children;
        _notifications = notifications;
        _unitOfWork = unitOfWork;
    }

    /// Cria uma missão a partir do payload da API.
    public async Task<TaskResponse> CreateAsync(
        CreateTaskRequest request, Guid createdById, CancellationToken ct)
    {
        // 1. Valida que a criança existe (regra de aplicação — não do domínio).
        var child = await _children.GetByIdAsync(request.AssignedTo, ct);
        if (child is null)
            throw new DomainException("Criança não encontrada.");

        // 2. Traduz o payload (strings) para os tipos do domínio.
        var category = EnumMapper.FromSnakeCase<TaskCategory>(request.Category);
        var priority = EnumMapper.FromSnakeCase<TaskPriority>(request.Priority);
        var date = DateOnly.Parse(request.ScheduledDate);
        var time = TimeOnly.Parse(request.ScheduledTime);
        int? duration = string.IsNullOrWhiteSpace(request.EstimatedDuration)
            ? null
            : int.Parse(request.EstimatedDuration);

        // 3. Cria a entidade (o CONSTRUTOR valida invariantes: título, data, etc.).
        var task = new TaskItem(
            request.Title, category, priority, date, time,
            request.AssignedTo, createdById,
            request.Description, duration);

        // 4. Rastreia a adição e confirma numa transação (Unit of Work).
        await _tasks.AddAsync(task, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return TaskResponse.From(task);
    }

    public async Task<IReadOnlyList<TaskResponse>> GetAllAsync(CancellationToken ct)
    {
        var tasks = await _tasks.ListAsync(ct);
        return tasks.Select(TaskResponse.From).ToList();
    }

    /// <summary>Missões criadas por um responsável (visão do pai).</summary>
    public async Task<IReadOnlyList<TaskResponse>> GetForParentAsync(Guid parentUserId, CancellationToken ct)
    {
        var tasks = await _tasks.ListAsync(ct);
        return tasks.Where(t => t.CreatedById == parentUserId).Select(TaskResponse.From).ToList();
    }

    /// <summary>Missões atribuídas à criança logada (visão da criança).</summary>
    public async Task<IReadOnlyList<TaskResponse>> GetForChildAsync(Guid childUserId, CancellationToken ct)
    {
        var child = await _children.GetByUserIdAsync(childUserId, ct);
        if (child is null) return Array.Empty<TaskResponse>();

        var tasks = await _tasks.ListAsync(ct);
        return tasks.Where(t => t.AssignedToId == child.Id).Select(TaskResponse.From).ToList();
    }

    public async Task<TaskResponse?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var task = await _tasks.GetByIdAsync(id, ct);
        return task is null ? null : TaskResponse.From(task);
    }

    /// <summary>
    /// Conclui uma missão E credita os pontos à criança.
    ///
    /// ATENÇÃO (gancho com a revisão): esta é a ORQUESTRAÇÃO entre agregados
    /// que discutimos no Desafio 2! A entidade TaskItem só muda SEU estado;
    /// a entidade Child só soma pontos. O serviço coordena as duas partes.
    /// </summary>
    public async Task<TaskResponse> CompleteAsync(Guid taskId, CancellationToken ct)
    {
        var task = await _tasks.GetByIdAsync(taskId, ct)
            ?? throw new DomainException("Missão não encontrada.");

        var child = await _children.GetByIdAsync(task.AssignedToId, ct)
            ?? throw new DomainException("Criança não encontrada.");

        // task.Complete() pode lançar DomainException (ex.: já concluída).
        // child.AddPoints() soma os pontos definidos pela prioridade da missão.
        task.Complete();
        child.AddPoints(task.RewardPoints);

        // Avisa o responsável que criou a missão que ela foi concluída.
        await _notifications.AddAsync(new Notification(
            $"{child.FullName} concluiu \"{task.Title}\" (+{task.RewardPoints} pts)",
            NotificationType.TaskCompleted,
            task.CreatedById), ct);

        // Avisa também a própria criança (se ela tem login próprio).
        if (child.UserId != Guid.Empty)
        {
            await _notifications.AddAsync(new Notification(
                $"Você concluiu \"{task.Title}\" e ganhou +{task.RewardPoints} pontos! 🎉",
                NotificationType.TaskCompleted,
                child.UserId), ct);
        }

        await _tasks.UpdateAsync(task, ct);
        await _children.UpdateAsync(child, ct);

        // UM SaveChanges confirma AMBAS as mudanças atomicamente.
        // Se algo falhar aqui, nada é gravado — consistência garantida (Desafio 2!).
        await _unitOfWork.SaveChangesAsync(ct);

        return TaskResponse.From(task);
    }

    /// <summary>
    /// Cancela/abandona uma missão (status Skipped). Não concede pontos.
    /// Avisa a criança de que a missão foi cancelada (se ela tem login próprio).
    /// </summary>
    public async Task<TaskResponse> SkipAsync(Guid taskId, CancellationToken ct)
    {
        var task = await _tasks.GetByIdAsync(taskId, ct)
            ?? throw new DomainException("Missão não encontrada.");

        var child = await _children.GetByIdAsync(task.AssignedToId, ct);

        // task.Skip() valida o estado (não pode cancelar já concluída/cancelada).
        task.Skip();

        // Avisa a criança de que a missão foi cancelada.
        if (child is not null && child.UserId != Guid.Empty)
        {
            await _notifications.AddAsync(new Notification(
                $"A missão \"{task.Title}\" foi cancelada.",
                NotificationType.TaskSkipped,
                child.UserId), ct);
        }

        await _tasks.UpdateAsync(task, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return TaskResponse.From(task);
    }
}
