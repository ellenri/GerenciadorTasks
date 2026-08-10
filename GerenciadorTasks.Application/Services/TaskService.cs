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

    /// <summary>
    /// Cria missão(ões) a partir do payload. Para recorrência semanal/2x, gera
    /// instâncias independentes (uma por ocorrência) agrupadas por RecurrenceGroupId,
    /// cobrindo as próximas 8 semanas a partir da data inicial.
    /// </summary>
    public async Task<IReadOnlyList<TaskResponse>> CreateAsync(
        CreateTaskRequest request, Guid createdById, CancellationToken ct)
    {
        // 1. Valida que a criança existe (regra de aplicação — não do domínio).
        var child = await _children.GetByIdAsync(request.AssignedTo, ct);
        if (child is null)
            throw new DomainException("Criança não encontrada.");

        // 2. Traduz o payload (strings) para os tipos do domínio.
        var category = EnumMapper.FromSnakeCase<TaskCategory>(request.Category);
        var priority = EnumMapper.FromSnakeCase<TaskPriority>(request.Priority);
        var time = TimeOnly.Parse(request.ScheduledTime);
        int? duration = string.IsNullOrWhiteSpace(request.EstimatedDuration)
            ? null
            : int.Parse(request.EstimatedDuration);

        // 3. Recorrência: computa as datas das ocorrências.
        var recurrence = EnumMapper.FromSnakeCase<RecurrenceType>(string.IsNullOrEmpty(request.RecurrenceType) ? "once" : request.RecurrenceType);
        var dates = ComputeOccurrenceDates(recurrence, request.ScheduledDate, request.RecurrenceDays);
        Guid? groupId = recurrence == RecurrenceType.Once ? null : Guid.NewGuid();

        // 4. Cria cada ocorrência (o CONSTRUTOR valida invariantes: título, data, etc.).
        var created = new List<TaskItem>();
        foreach (var date in dates)
        {
            var task = new TaskItem(
                request.Title, category, priority, date, time,
                request.AssignedTo, createdById,
                request.Description, duration);
            if (groupId.HasValue)
                task.SetRecurrenceGroup(groupId.Value);
            await _tasks.AddAsync(task, ct);
            created.Add(task);
        }

        // 5. Rastreia as adições e confirma numa transação (Unit of Work).
        await _unitOfWork.SaveChangesAsync(ct);

        return created.Select(TaskResponse.From).ToList();
    }

    /// <summary>
    /// Computa as datas das ocorrências conforme o padrão de recorrência:
    ///  - Once: apenas a data informada.
    ///  - Weekly: 1 dia da semana, pelas próximas 8 semanas.
    ///  - TwiceWeekly: 2 dias da semana, 8 ocorrências de cada (8 semanas).
    /// </summary>
    private static List<DateOnly> ComputeOccurrenceDates(RecurrenceType type, string scheduledDate, int[]? days)
    {
        var start = DateOnly.Parse(scheduledDate);
        if (type == RecurrenceType.Once)
            return new List<DateOnly> { start };

        var daySet = (days ?? Array.Empty<int>())
            .Select(d => (DayOfWeek)d)
            .ToHashSet();
        int expected = type == RecurrenceType.Weekly ? 1 : 2;
        if (daySet.Count != expected)
            throw new DomainException($"Para repetição ‘{EnumMapper.ToSnakeCase(type)}’, marque exatamente {expected} dia(s) da semana.");

        // Gera 8 ocorrências por dia (cobre 8 semanas), a partir da data inicial.
        var dates = new List<DateOnly>();
        var countByDay = daySet.ToDictionary(d => d, _ => 0);
        var d = start;
        var safety = start.AddDays(365);
        while (countByDay.Values.Any(c => c < 8))
        {
            if (daySet.Contains(d.DayOfWeek))
            {
                dates.Add(d);
                countByDay[d.DayOfWeek]++;
            }
            d = d.AddDays(1);
            if (d > safety) break;
        }
        return dates;
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
    /// A criança envia a foto de comprovação. A missão passa a aguardar aprovação
    /// do responsável (PendingReview). O responsável é notificado para analisar.
    /// </summary>
    public async Task<TaskResponse> SubmitAsync(Guid taskId, string imageUrl, CancellationToken ct)
    {
        var task = await _tasks.GetByIdAsync(taskId, ct)
            ?? throw new DomainException("Missão não encontrada.");

        var child = await _children.GetByIdAsync(task.AssignedToId, ct);

        task.SubmitForReview(imageUrl);

        // Avisa o responsável que há uma comprovação para analisar.
        await _notifications.AddAsync(new Notification(
            $"{child?.FullName ?? "A criança"} enviou a comprovação de \"{task.Title}\".",
            NotificationType.TaskSubmitted,
            task.CreatedById), ct);

        await _tasks.UpdateAsync(task, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return TaskResponse.From(task);
    }

    /// <summary>
    /// O responsável aprova a comprovação: conclui a missão E credita os pontos.
    /// </summary>
    public async Task<TaskResponse> ApproveAsync(Guid taskId, CancellationToken ct)
    {
        var task = await _tasks.GetByIdAsync(taskId, ct)
            ?? throw new DomainException("Missão não encontrada.");

        var child = await _children.GetByIdAsync(task.AssignedToId, ct)
            ?? throw new DomainException("Criança não encontrada.");

        task.Approve();
        child.AddPoints(task.RewardPoints);

        await _notifications.AddAsync(new Notification(
            $"{child.FullName} concluiu \"{task.Title}\" (+{task.RewardPoints} pts)",
            NotificationType.TaskCompleted,
            task.CreatedById), ct);

        if (child.UserId != Guid.Empty)
        {
            await _notifications.AddAsync(new Notification(
                $"Sua missão \"{task.Title}\" foi aprovada! +{task.RewardPoints} pontos 🎉",
                NotificationType.TaskCompleted,
                child.UserId), ct);
        }

        await _tasks.UpdateAsync(task, ct);
        await _children.UpdateAsync(child, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return TaskResponse.From(task);
    }

    /// <summary>
    /// O responsável rejeita a comprovação com um comentário. A missão volta para
    /// InProgress e o comentário fica visível para a criança refazer.
    /// </summary>
    public async Task<TaskResponse> RejectAsync(Guid taskId, string comment, CancellationToken ct)
    {
        var task = await _tasks.GetByIdAsync(taskId, ct)
            ?? throw new DomainException("Missão não encontrada.");

        var child = await _children.GetByIdAsync(task.AssignedToId, ct);

        task.Reject(comment);

        if (child is not null && child.UserId != Guid.Empty)
        {
            await _notifications.AddAsync(new Notification(
                $"Revise a missão \"{task.Title}\": {comment}",
                NotificationType.TaskRejected,
                child.UserId), ct);
        }

        await _tasks.UpdateAsync(task, ct);
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
