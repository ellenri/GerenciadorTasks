using GerenciadorTasks.Core.Entities;

namespace GerenciadorTasks.Application.Abstractions;

/// <summary>Contrato de acesso a dados para crianças.</summary>
public interface IChildRepository
{
    Task<Child?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Child?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Child>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Child>> ListByParentAsync(Guid parentUserId, CancellationToken ct = default);
    Task AddAsync(Child child, CancellationToken ct = default);
    Task UpdateAsync(Child child, CancellationToken ct = default);
}
