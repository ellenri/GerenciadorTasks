using GerenciadorTasks.Application.Abstractions;
using GerenciadorTasks.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorTasks.Infrastructure.Persistence;

/// <summary>Implementação EF Core de IChildRepository.</summary>
public class EfChildRepository : IChildRepository
{
    private readonly AppDbContext _db;

    public EfChildRepository(AppDbContext db) => _db = db;

    public async Task<Child?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Children.FindAsync(new object[] { id }, ct);

    public async Task<IReadOnlyList<Child>> ListAsync(CancellationToken ct = default)
        => await _db.Children.AsNoTracking().ToListAsync(ct);

    public Task AddAsync(Child child, CancellationToken ct = default)
    {
        _db.Children.Add(child);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Child child, CancellationToken ct = default)
    {
        _db.Children.Update(child);
        return Task.CompletedTask;
    }
}
