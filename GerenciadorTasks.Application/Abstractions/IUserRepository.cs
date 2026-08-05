using GerenciadorTasks.Core.Entities;

namespace GerenciadorTasks.Application.Abstractions;

/// <summary>Contrato de acesso a dados para usuários (responsáveis).</summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
}
