namespace GerenciadorTasks.Core.Enums;

/// <summary>
/// Ciclo de vida de uma missão.
/// Pending -> InProgress -> PendingReview -> Completed (aprovada pelo responsável)
/// ou de volta a InProgress (rejeitada com comentario, para a criança refazer).
/// Skipped = abandonada/cancelada.
///
/// IMPORTANTE: os valores numericos de Pending/InProgress/Completed/Skipped
/// NAO mudam (sao persistidos no banco como int). PendingReview foi adicionado
/// com um valor novo (5) para preservar os dados existentes.
/// </summary>
public enum TaskStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Skipped = 4,
    PendingReview = 5, // criança enviou comprovação; aguarda aprovação do responsável
}
