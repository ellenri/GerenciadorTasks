using GerenciadorTasks.Application.Abstractions;
using GerenciadorTasks.Application.Dtos;
using GerenciadorTasks.Core.Entities;
using GerenciadorTasks.Core.Exceptions;

namespace GerenciadorTasks.Application.Services;

/// <summary>
/// Casos de uso de notificações: listar, contar não-lidas e marcar como lida.
/// As notificações em si são criadas pelos serviços de Task/Reward em eventos chave
/// (concluir missão, resgatar recompensa).
/// </summary>
public class NotificationService
{
    private readonly INotificationRepository _notifications;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(INotificationRepository notifications, IUnitOfWork unitOfWork)
    {
        _notifications = notifications;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<NotificationResponse>> GetForUserAsync(Guid userId, CancellationToken ct)
    {
        var list = await _notifications.GetByUserIdAsync(userId, ct);
        return list.Select(NotificationResponse.From).ToList();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct)
        => await _notifications.CountUnreadAsync(userId, ct);

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct)
    {
        var n = await _notifications.GetByIdAsync(notificationId, ct)
            ?? throw new DomainException("Notificação não encontrada.");

        // Só o dono pode marcar a própria notificação (isolamento entre usuários).
        if (n.UserId != userId)
            throw new DomainException("Notificação não pertence ao usuário.");

        n.MarkAsRead();
        await _notifications.UpdateAsync(n, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
