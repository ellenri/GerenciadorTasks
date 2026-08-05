using GerenciadorTasks.Core.Entities;

namespace GerenciadorTasks.Application.Abstractions;

/// <summary>Contrato de acesso a dados para recompensas.</summary>
public interface IRewardRepository
{
    Task<Reward?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Reward>> ListAsync(CancellationToken ct = default);
    Task AddAsync(Reward reward, CancellationToken ct = default);
    Task UpdateAsync(Reward reward, CancellationToken ct = default);
}
