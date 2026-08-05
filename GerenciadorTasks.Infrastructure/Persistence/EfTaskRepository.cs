using GerenciadorTasks.Application.Abstractions;
using GerenciadorTasks.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorTasks.Infrastructure.Persistence;

/// <summary>
/// Implementação EF Core de ITaskRepository — fala com o banco SQLite.
///
/// Detalhe crucial: NENHUM método chama SaveChanges aqui. O repositório só
/// RASTREIA as mudanças no DbContext. Quem confirma é o IUnitOfWork (no fim
/// da operação, no serviço). Assim várias mudanças viram uma transação só.
/// </summary>
public class EfTaskRepository : ITaskRepository
{
    private readonly AppDbContext _db;

    public EfTaskRepository(AppDbContext db) => _db = db;

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Tasks.FindAsync(new object[] { id }, ct);

    public async Task<IReadOnlyList<TaskItem>> ListAsync(CancellationToken ct = default)
        // AsNoTracking: leitura sem rastreamento (mais rápido, não vamos modificar).
        => await _db.Tasks.AsNoTracking().ToListAsync(ct);

    public Task AddAsync(TaskItem task, CancellationToken ct = default)
    {
        _db.Tasks.Add(task); // rastreia como "Added"; grava no SaveChanges
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TaskItem task, CancellationToken ct = default)
    {
        _db.Tasks.Update(task); // marca como "Modified"
        return Task.CompletedTask;
    }
}
