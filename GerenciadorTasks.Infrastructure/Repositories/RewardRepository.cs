using GerenciadorTasks.Core.Entities;
using GerenciadorTasks.Core.Interfaces.Repositories;
using GerenciadorTasks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorTasks.Infrastructure.Repositories;

public class RewardRepository : IRewardRepository
{
    private readonly AppDbContext _context;

    public RewardRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Reward?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Rewards
            .Include(r => r.CreatedBy)
            .Include(r => r.RedeemedBy)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Reward>> GetByCreatedByIdAsync(Guid createdById, CancellationToken cancellationToken = default)
    {
        return await _context.Rewards
            .Include(r => r.RedeemedBy)
            .Where(r => r.CreatedById == createdById)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reward>> GetAvailableRewardsAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await _context.Rewards
            .Where(r => r.CreatedById == parentId && r.RedeemedById == null)
            .OrderBy(r => r.RequiredPoints)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Reward reward, CancellationToken cancellationToken = default)
    {
        await _context.Rewards.AddAsync(reward, cancellationToken);
    }

    public void Update(Reward reward)
    {
        _context.Rewards.Update(reward);
    }
}
