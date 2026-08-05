using GerenciadorTasks.Core.Exceptions;

namespace GerenciadorTasks.Core.Entities;

/// <summary>
/// Justificativa (ex.: para não concluir/abandonar uma missão) associada a uma
/// TaskItem. Entidade isolada — ainda sem fluxo de uso; pronta para quando o
/// domínio cobrir aprovação/rejeição de justificativas.
/// </summary>
public class Justification : BaseEntity
{
    public string Reason { get; private set; } = null!;
    public bool IsApproved { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    public Guid TaskItemId { get; private set; }

    private Justification() { }

    public Justification(string reason, Guid taskItemId)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Justificativa é obrigatória.");
        if (taskItemId == Guid.Empty)
            throw new DomainException("A justificativa precisa estar associada a uma missão.");

        Reason = reason.Trim();
        TaskItemId = taskItemId;
        IsApproved = false;
    }

    public void Approve()
    {
        if (IsApproved)
            throw new DomainException("Justificativa já foi aprovada.");

        IsApproved = true;
        ReviewedAt = DateTime.UtcNow;
        Touch();
    }

    public void Reject()
    {
        if (IsApproved)
            throw new DomainException("Justificativa já foi aprovada, não pode ser rejeitada.");

        ReviewedAt = DateTime.UtcNow;
        Touch();
    }
}
