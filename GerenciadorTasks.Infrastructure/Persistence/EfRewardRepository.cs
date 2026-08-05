using GerenciadorTasks.Application.Abstractions;
using GerenciadorTasks.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorTasks.Infrastructure.Persistence;

/// <summary>Implementação EF Core de IRewardRepository.</summary>
public class EfRewardRepository : IRewardRepository
{
    private readonly AppDbContext _db;

    public EfRewardRepository(AppDbContext db) => _db = db;

    public async Task<Reward?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Rewards.FindAsync(new object[] { id }, ct);

    public async Task<IReadOnlyList<Reward>> ListAsync(CancellationToken ct = default)
        => await _db.Rewards.AsNoTracking().ToListAsync(ct);

    public Task AddAsync(Reward reward, CancellationToken ct = default)
    {
        _db.Rewards.Add(reward);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Reward reward, CancellationToken ct = default)
    {
        _db.Rewards.Update(reward);
        return Task.CompletedTask;
    }
}
