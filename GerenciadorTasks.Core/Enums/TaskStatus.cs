namespace GerenciadorTasks.Core.Enums;

/// <summary>
/// Ciclo de vida de uma missão.
/// Pending -> InProgress -> Completed (fluxo feliz) ou Skipped (abandono).
/// </summary>
public enum TaskStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Skipped = 4
}
