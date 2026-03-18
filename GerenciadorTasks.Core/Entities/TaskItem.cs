using GerenciadorTasks.Core.Enums;
using GerenciadorTasks.Core.Exceptions;
using TaskStatus = GerenciadorTasks.Core.Enums.TaskStatus;

namespace GerenciadorTasks.Core.Entities;

public class TaskItem : BaseEntity
{
    public string Title { get; private set; }
    public string Description { get; private set; }
    public TaskStatus Status { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public int RewardPoints { get; private set; }
    public string? PhotoProofUrl { get; private set; }

    public Guid CreatedById { get; private set; }
    public User CreatedBy { get; private set; } = null!;

    public Guid AssignedToId { get; private set; }
    public User AssignedTo { get; private set; } = null!;

    public Justification? Justification { get; private set; }

    private TaskItem() { }

    public TaskItem(string title, string description, Guid createdById, Guid assignedToId, DateTime? dueDate, int rewardPoints)
    {
        Validate(title, description, rewardPoints);

        Title = title;
        Description = description;
        Status = TaskStatus.Pending;
        DueDate = dueDate;
        RewardPoints = rewardPoints;
        CreatedById = createdById;
        AssignedToId = assignedToId;
    }

    public void Start()
    {
        if (Status != TaskStatus.Pending)
            throw new DomainException("Apenas tarefas pendentes podem ser iniciadas.");

        Status = TaskStatus.InProgress;
        SetUpdated();
    }

    public void Complete(string photoProofUrl)
    {
        if (Status != TaskStatus.InProgress)
            throw new DomainException("Apenas tarefas em andamento podem ser concluídas.");

        if (string.IsNullOrWhiteSpace(photoProofUrl))
            throw new DomainException("Foto de comprovação é obrigatória.");

        Status = TaskStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        PhotoProofUrl = photoProofUrl;
        SetUpdated();
    }

    public void Reject()
    {
        if (Status != TaskStatus.Completed)
            throw new DomainException("Apenas tarefas concluídas podem ser rejeitadas.");

        Status = TaskStatus.Rejected;
        CompletedAt = null;
        PhotoProofUrl = null;
        SetUpdated();
    }

    public void Justify(string reason)
    {
        if (Status != TaskStatus.InProgress && Status != TaskStatus.Pending)
            throw new DomainException("Tarefas concluídas ou rejeitadas não podem ser justificadas.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Justificativa é obrigatória.");

        Justification = new Justification(reason, Id);
        Status = TaskStatus.Justified;
        SetUpdated();
    }

    public void ApproveJustification()
    {
        if (Status != TaskStatus.Justified || Justification is null)
            throw new DomainException("Não há justificativa pendente para aprovar.");

        Justification.Approve();
        Status = TaskStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        AssignedTo.AddPoints(RewardPoints);
        SetUpdated();
    }

    public void RejectJustification()
    {
        if (Status != TaskStatus.Justified || Justification is null)
            throw new DomainException("Não há justificativa pendente para rejeitar.");

        Justification.Reject();
        Status = TaskStatus.Rejected;
        SetUpdated();
    }

    private static void Validate(string title, string description, int rewardPoints)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Título é obrigatório.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Descrição é obrigatória.");

        if (rewardPoints < 0)
            throw new DomainException("Pontos de recompensa não podem ser negativos.");
    }
}
