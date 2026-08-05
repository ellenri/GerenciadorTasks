using GerenciadorTasks.Core.Entities;

namespace GerenciadorTasks.Application.Dtos;

/// <summary>Notificação devolvida pela API.</summary>
public record NotificationResponse(
    Guid Id,
    string Message,
    string Type,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt)
{
    public static NotificationResponse From(Notification n) => new(
        n.Id,
        n.Message,
        n.Type.ToString(),
        n.IsRead,
        n.ReadAt,
        n.CreatedAt);
}
