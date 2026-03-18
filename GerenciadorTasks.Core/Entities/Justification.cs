using GerenciadorTasks.Core.Enums;
using GerenciadorTasks.Core.Exceptions;

namespace GerenciadorTasks.Core.Entities;

public class Justification : BaseEntity
{
    public string Reason { get; private set; }
    public bool IsApproved { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    public Guid TaskItemId { get; private set; }
    public TaskItem TaskItem { get; private set; } = null!;

    private Justification() { }

    public Justification(string reason, Guid taskItemId)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Justificativa é obrigatória.");

        Reason = reason;
        TaskItemId = taskItemId;
        IsApproved = false;
    }

    public void Approve()
    {
        if (IsApproved)
            throw new DomainException("Justificativa já foi aprovada.");

        IsApproved = true;
        ReviewedAt = DateTime.UtcNow;
        SetUpdated();
    }

    public void Reject()
    {
        if (IsApproved)
            throw new DomainException("Justificativa já foi aprovada, não pode ser rejeitada.");

        ReviewedAt = DateTime.UtcNow;
        SetUpdated();
    }
}
