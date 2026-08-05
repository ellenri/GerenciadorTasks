using GerenciadorTasks.Core.Enums;
using GerenciadorTasks.Core.Exceptions;

// Alias: desambigua de System.Threading.Tasks.TaskStatus (que também existe no .NET).
using TaskStatus = GerenciadorTasks.Core.Enums.TaskStatus;

namespace GerenciadorTasks.Core.Entities;

/// <summary>
/// Uma "missão" (tarefa) atribuída a uma criança por um responsável.
///
/// Chama-se TaskItem (e não Task) porque "Task" já é um tipo fundamental do .NET
/// (System.Threading.Tasks.Task). Reutilizar o nome causaria confusão e bugs.
/// </summary>
public class TaskItem : BaseEntity
{
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public TaskCategory Category { get; private set; }
    public TaskPriority Priority { get; private set; }
    public TaskStatus Status { get; private set; }
    public DateOnly ScheduledDate { get; private set; }
    public TimeOnly ScheduledTime { get; private set; }
    public int? EstimatedDurationMinutes { get; private set; }

    public Guid AssignedToId { get; private set; }   // FK -> Child
    public Guid CreatedById { get; private set; }     // FK -> User (responsável)

    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Pontos de recompensa por concluir a missão, definidos pela prioridade.
    /// É uma propriedade calculada (get-only) — não há setter porque a regra é do domínio.
    /// </summary>
    public int RewardPoints => Priority switch
    {
        TaskPriority.Low => 10,
        TaskPriority.Medium => 20,
        TaskPriority.High => 30,
        _ => 0
    };

    // Construtor privado: só o EF Core usa (materialização a partir do banco).
    private TaskItem() : base() { }

    public TaskItem(
        string title,
        TaskCategory category,
        TaskPriority priority,
        DateOnly scheduledDate,
        TimeOnly scheduledTime,
        Guid assignedToId,
        Guid createdById,
        string? description = null,
        int? estimatedDurationMinutes = null)
        : base()
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("O título da missão é obrigatório.");
        if (assignedToId == Guid.Empty)
            throw new DomainException("A missão precisa estar atribuída a uma criança.");
        if (createdById == Guid.Empty)
            throw new DomainException("A missão precisa ter um responsável.");
        if (scheduledDate < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new DomainException("A data da missão não pode estar no passado.");

        Title = title.Trim();
        Category = category;
        Priority = priority;
        ScheduledDate = scheduledDate;
        ScheduledTime = scheduledTime;
        AssignedToId = assignedToId;
        CreatedById = createdById;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        EstimatedDurationMinutes = estimatedDurationMinutes;
        Status = TaskStatus.Pending; // toda missão nova começa pendente
    }

    /// Inicia a missão. Só faz sentido partindo de Pending.
    public void Start()
    {
        if (Status != TaskStatus.Pending)
            throw new DomainException($"Não é possível iniciar uma missão com status '{Status}'.");

        Status = TaskStatus.InProgress;
        Touch();
    }

    /// <summary>
    /// Conclui a missão. A camada de aplicação deve ler RewardPoints
    /// e creditá-los à criança (Child.AddPoints) — o domínio orquestra o
    /// estado, a aplicação orquestra a transação entre agregados.
    /// </summary>
    public void Complete()
    {
        if (Status == TaskStatus.Completed)
            throw new DomainException("A missão já foi concluída.");
        if (Status == TaskStatus.Skipped)
            throw new DomainException("Não é possível concluir uma missão abandonada.");

        Justification.Approve();
        Status = TaskStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        Touch();
    }

    /// Abandona/pula a missão. Não concede pontos.
    public void Skip()
    {
        if (Status == TaskStatus.Completed)
            throw new DomainException("Não é possível abandonar uma missão já concluída.");

        Status = TaskStatus.Skipped;
        Touch();
    }
}
