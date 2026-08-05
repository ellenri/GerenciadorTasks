using GerenciadorTasks.Core.Enums;
using GerenciadorTasks.Core.Exceptions;

namespace GerenciadorTasks.Core.Entities;

/// <summary>
/// Notificação direcionada a um usuário (responsável). Ainda sem fluxo de envio —
/// a entidade existe pronta para quando houver notificações reais.
/// </summary>
public class Notification : BaseEntity
{
    public string Message { get; private set; } = null!;
    public NotificationType Type { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }

    public Guid UserId { get; private set; }

    private Notification() { }

    public Notification(string message, NotificationType type, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new DomainException("Mensagem da notificação é obrigatória.");
        if (userId == Guid.Empty)
            throw new DomainException("A notificação precisa estar associada a um usuário.");

        Message = message.Trim();
        Type = type;
        UserId = userId;
        IsRead = false;
    }

    public void MarkAsRead()
    {
        if (IsRead) return;

        IsRead = true;
        ReadAt = DateTime.UtcNow;
        Touch();
    }
}
