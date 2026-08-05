using GerenciadorTasks.Core.Entities;

namespace GerenciadorTasks.Application.Abstractions;

/// <summary>
/// Contrato de acesso a dados para tarefas (missões).
///
/// O Application só conhece a INTERFACE — nunca a implementação concreta
/// (que pode ser em memória, SQLite, etc.). Isso é a Inversão de Dependência.
/// </summary>
public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TaskItem>> ListAsync(CancellationToken ct = default);
    Task AddAsync(TaskItem task, CancellationToken ct = default);
    Task UpdateAsync(TaskItem task, CancellationToken ct = default);
}
