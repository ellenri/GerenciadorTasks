using GerenciadorTasks.Core.Entities;

namespace GerenciadorTasks.Core.Interfaces.Repositories;

public interface IRewardRepository
{
    Task<Reward?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reward>> GetByCreatedByIdAsync(Guid createdById, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reward>> GetAvailableRewardsAsync(Guid parentId, CancellationToken cancellationToken = default);
    Task AddAsync(Reward reward, CancellationToken cancellationToken = default);
    void Update(Reward reward);
}
