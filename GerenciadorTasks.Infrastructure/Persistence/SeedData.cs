using GerenciadorTasks.Application.Abstractions;
using GerenciadorTasks.Core.Entities;
using GerenciadorTasks.Core.Enums;

namespace GerenciadorTasks.Infrastructure.Persistence;

/// <summary>
/// Dados iniciais (seed) — idempotente: pode rodar várias vezes sem duplicar.
/// Cria um responsável padrão (Parent) para facilitar os testes em desenvolvimento.
/// </summary>
public static class SeedData
{
    /// Credenciais do responsável padrão (apenas para desenvolvimento).
    public const string DefaultEmail = "responsavel@exemplo.com";
    public const string DefaultPassword = "123456";

    public static async Task InitializeAsync(
        IUserRepository users,
        IChildRepository children,
        IPasswordHasher hasher,
        IUnitOfWork unitOfWork,
        CancellationToken ct = default)
    {
        // 1. Responsável padrão (dev) — para você conseguir logar de imediato.
        if (await users.GetByEmailAsync(DefaultEmail, ct) is null)
        {
            var parent = new User("Responsável Padrão", DefaultEmail, UserRole.Parent);
            parent.SetPasswordHash(hasher.Hash(DefaultPassword));
            await users.AddAsync(parent, ct);
        }

        // 2. Crianças iniciais (apenas se a tabela estiver vazia).
        if ((await children.ListAsync(ct)).Count == 0)
        {
            await children.AddAsync(new Child("João Silva", new DateOnly(2015, 3, 15), "/avatars/boy1.png"), ct);
            await children.AddAsync(new Child("Maria Silva", new DateOnly(2017, 7, 22), "/avatars/girl_blondehair.png"), ct);
            await children.AddAsync(new Child("Pedro Silva", new DateOnly(2019, 11, 8), "/avatars/boy_cap.png"), ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
