using GerenciadorTasks.Application.Abstractions;
using GerenciadorTasks.Core.Entities;
using GerenciadorTasks.Core.Enums;

namespace GerenciadorTasks.Infrastructure.Persistence;

/// <summary>
/// Dados iniciais (seed) — idempotente: pode rodar várias vezes sem duplicar.
/// Cria um responsável padrão (Parent), as crianças iniciais e um catálogo de
/// recompensas de exemplo para facilitar os testes em desenvolvimento.
/// </summary>
public static class SeedData
{
    /// Credenciais do responsável padrão (apenas para desenvolvimento).
    public const string DefaultEmail = "responsavel@exemplo.com";
    public const string DefaultPassword = "123456";

    public static async Task InitializeAsync(
        IUserRepository users,
        IChildRepository children,
        IRewardRepository rewards,
        IPasswordHasher hasher,
        IUnitOfWork unitOfWork,
        CancellationToken ct = default)
    {
        // 1. Responsável padrão (dev) — para você conseguir logar de imediato.
        User? parent = await users.GetByEmailAsync(DefaultEmail, ct);
        if (parent is null)
        {
            parent = new User("Responsável Padrão", DefaultEmail, UserRole.Parent);
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

        // 3. Catálogo de recompensas de exemplo (criado pelo responsável padrão).
        if ((await rewards.ListAsync(ct)).Count == 0 && parent is not null)
        {
            await rewards.AddAsync(new Reward("30 min de videogame", "Meia hora do jogo favorito", 50, parent.Id), ct);
            await rewards.AddAsync(new Reward("Escolher o filme da noite", "A criança escolhe o filme da família", 100, parent.Id), ct);
            await rewards.AddAsync(new Reward("Passeio especial", "Ida ao parque ou à sorveteria", 200, parent.Id), ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
