namespace GerenciadorTasks.Core.Enums;

public enum NotificationType
{
    TaskAssigned,
    TaskCompleted,
    TaskRejected,
    TaskJustified,
    RewardUnlocked,
    RewardRedeemed,
    TaskSkipped,
    TaskSubmitted,
    /// <summary>Lembrete agendado (pelo ReminderService) para a criança fazer a missão.</summary>
    TaskReminder
}
