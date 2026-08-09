using System.Collections.Concurrent;
using GerenciadorTasks.Application.Abstractions;
using GerenciadorTasks.Core.Entities;

namespace GerenciadorTasks.Infrastructure.Persistence;

/// <summary>Implementação EM MEMÓRIA de IChildRepository.</summary>
public class InMemoryChildRepository : IChildRepository
{
    private readonly ConcurrentDictionary<Guid, Child> _store = new();

    public Task<Child?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.TryGetValue(id, out var child) ? child : null);

    public Task<Child?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(_store.Values.FirstOrDefault(c => c.UserId == userId));

    public Task<IReadOnlyList<Child>> ListAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Child>>(_store.Values.ToList());

    public Task<IReadOnlyList<Child>> ListByParentAsync(Guid parentUserId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Child>>(_store.Values.Where(c => c.ParentUserId == parentUserId).ToList());

    public Task AddAsync(Child child, CancellationToken ct = default)
    {
        _store[child.Id] = child;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Child child, CancellationToken ct = default)
    {
        _store[child.Id] = child;
        return Task.CompletedTask;
    }
}
