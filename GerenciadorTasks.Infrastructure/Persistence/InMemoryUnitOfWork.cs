using GerenciadorTasks.Application.Abstractions;

namespace GerenciadorTasks.Infrastructure.Persistence;

/// <summary>
/// Unit of Work "vazio": não faz nada. Usado quando os repositórios são em MEMÓRIA
/// (que já gravam imediatamente, sem precisar de confirmação).
///
/// Mantemos esta classe para o swap ficar limpo: trocar SQLite ↔ memória é só
/// mudar o registro no Program.cs, sem mexer nos serviços.
/// </summary>
public class InMemoryUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => Task.FromResult(0); // nada a confirmar
}
