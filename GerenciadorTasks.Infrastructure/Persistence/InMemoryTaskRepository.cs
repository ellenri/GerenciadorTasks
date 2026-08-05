using System.Collections.Concurrent;
using GerenciadorTasks.Application.Abstractions;
using GerenciadorTasks.Core.Entities;

namespace GerenciadorTasks.Infrastructure.Persistence;

/// <summary>
/// Implementação EM MEMÓRIA de ITaskRepository.
///
/// Usa ConcurrentDictionary para ser thread-safe (vários requests simultâneos).
/// Os dados somem ao reiniciar a aplicação — proposital para esta etapa.
/// Na próxima rodada trocaremos por SQLite + EF Core, sem mudar a interface!
/// </summary>
public class InMemoryTaskRepository : ITaskRepository
{
    private readonly ConcurrentDictionary<Guid, TaskItem> _store = new();

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.TryGetValue(id, out var task) ? task : null);

    public Task<IReadOnlyList<TaskItem>> ListAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<TaskItem>>(_store.Values.ToList());

    public Task AddAsync(TaskItem task, CancellationToken ct = default)
    {
        _store[task.Id] = task;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TaskItem task, CancellationToken ct = default)
    {
        _store[task.Id] = task;
        return Task.CompletedTask;
    }
}
