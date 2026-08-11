using GerenciadorTasks.Application.Abstractions;
using GerenciadorTasks.Core.Entities;
using GerenciadorTasks.Core.Enums;

// Alias: desambigua de System.Threading.Tasks.TaskStatus (que também existe no .NET).
using TaskStatus = GerenciadorTasks.Core.Enums.TaskStatus;

namespace GerenciadorTasks.Application.Services;

/// <summary>
/// Caso de uso de LEmbretes agendados ("avisar X min antes" e "na hora").
///
/// As notificações comuns são reativas (criadas no instante de um evento).
/// Lembrete é diferente: precisa disparar num HORARIO. Como não há um broker
/// de mensagens, um agendador em background (ReminderHostedService, na API)
/// chama <see cref="ProcessDueRemindersAsync"/> periodicamente; este serviço
/// encontra as missões cujo horário chegou e emite as notificações
/// (NotificationType.TaskReminder) para a criança, marcando o envio para não
/// repetir (idempotência via ReminderBeforeSentAt / ReminderAtStartSentAt).
/// </summary>
public class ReminderService
{
    private readonly ITaskRepository _tasks;
    private readonly IChildRepository _children;
    private readonly INotificationRepository _notifications;
    private readonly IUnitOfWork _unitOfWork;

    public ReminderService(
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
    /// Processa todos os lembretes devidos até agora: para cada missão candidata,
    /// dispara "antes" e/ou "na hora" conforme a configuração e o horário atual.
    /// Tudo numa única transação (Unit of Work) confirmada no fim.
    /// </summary>
    public async Task ProcessDueRemindersAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var candidates = await _tasks.ListWithRemindersAsync(ct);

        foreach (var task in candidates)
        {
            // Só lembra de missões a fazer. Concluída/cancelada = sem sentido.
            // PendingReview: a criança já enviou a comprovação — nada a cobrar.
            if (task.Status is TaskStatus.Completed or TaskStatus.Skipped or TaskStatus.PendingReview)
                continue;

            // Precisa da criança (e do login dela) para endereçar a notificação.
            var child = await _children.GetByIdAsync(task.AssignedToId, ct);
            if (child is null || child.UserId == Guid.Empty)
                continue;

            var scheduledUtc = ToUtc(task.ScheduledDate, task.ScheduledTime);

            // "Antes": dispara em (horário marcado − minutos).
            if (task.ReminderMinutesBefore is int minutes && task.ReminderBeforeSentAt is null)
            {
                if (now >= scheduledUtc.AddMinutes(-minutes))
                {
                    await EmitAsync(task, child.UserId,
                        $"⏰ Falta pouco! Em {minutes} min começa a missão “{task.Title}”.",
                        t => t.MarkReminderBeforeSent(), ct);
                }
            }

            // "Na hora": dispara no horário marcado.
            if (task.RemindAtStart && task.ReminderAtStartSentAt is null)
            {
                if (now >= scheduledUtc)
                {
                    await EmitAsync(task, child.UserId,
                        $"⏰ Agora é a hora da missão “{task.Title}”!",
                        t => t.MarkReminderAtStartSent(), ct);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <summary>Cria a notificação, marca o envio na entidade e rastreia a atualização.</summary>
    private async Task EmitAsync(
        TaskItem task,
        Guid userId,
        string message,
        Action<TaskItem> markSent,
        CancellationToken ct)
    {
        await _notifications.AddAsync(
            new Notification(message, NotificationType.TaskReminder, userId), ct);
        markSent(task);
        await _tasks.UpdateAsync(task, ct);
    }

    /// <summary>
    /// Converte a data/hora AGENDADA (que o pai digitou em horário local) para UTC,
    /// assumindo o fuso do próprio servidor. Adequado para um app de fuso único;
    /// se houver múltiplos fusos no futuro, trocar por um TimeZoneInfo configurável.
    /// </summary>
    private static DateTime ToUtc(DateOnly date, TimeOnly time)
    {
        var local = new DateTime(date, time);
        return DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime();
    }
}
