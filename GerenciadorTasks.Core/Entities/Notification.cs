using GerenciadorTasks.Core.Enums;

namespace GerenciadorTasks.Core.Entities;

public class Notification : BaseEntity
{
    public string Message { get; private set; }
    public NotificationType Type { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    private Notification() { }

    public Notification(string message, NotificationType type, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Mensagem da notificação é obrigatória.", nameof(message));

        Message = message;
        Type = type;
        UserId = userId;
        IsRead = false;
    }

    public void MarkAsRead()
    {
        if (IsRead) return;

        IsRead = true;
        ReadAt = DateTime.UtcNow;
        SetUpdated();
    }
}
