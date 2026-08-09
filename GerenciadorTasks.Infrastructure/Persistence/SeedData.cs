using GerenciadorTasks.Application.Abstractions;
using GerenciadorTasks.Core.Entities;
using GerenciadorTasks.Core.Enums;

namespace GerenciadorTasks.Infrastructure.Persistence;

/// <summary>
/// Dados iniciais (seed) — idempotente: pode rodar várias vezes sem duplicar.
///
/// Cria:
///  - um responsável padrão (Parent);
///  - crianças de exemplo, cada uma com o seu próprio login (User{Role=Child})
///    vinculado ao responsável;
///  - um catálogo de recompensas de exemplo (criado pelo responsável).
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
        //    Cada uma ganha um login próprio (User{Role=Child}) vinculado ao responsável.
        if ((await children.ListAsync(ct)).Count == 0)
        {
            await CreateChildWithLogin(users, children, hasher, unitOfWork,
                "João Silva", new DateOnly(2015, 3, 15), "/avatars/boy1.png",
                "joao@exemplo.com", parent.Id, ct);
            await CreateChildWithLogin(users, children, hasher, unitOfWork,
                "Maria Silva", new DateOnly(2017, 7, 22), "/avatars/girl_blondehair.png",
                "maria@exemplo.com", parent.Id, ct);
            await CreateChildWithLogin(users, children, hasher, unitOfWork,
                "Pedro Silva", new DateOnly(2019, 11, 8), "/avatars/boy_cap.png",
                "pedro@exemplo.com", parent.Id, ct);
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

    /// <summary>
    /// Cria o login da criança (User{Role=Child}) e o perfil (Child) vinculado,
    /// confirmados numa única transação. Usado apenas no seed.
    /// </summary>
    private static async Task CreateChildWithLogin(
        IUserRepository users,
        IChildRepository children,
        IPasswordHasher hasher,
        IUnitOfWork unitOfWork,
        string fullName,
        DateOnly birthDate,
        string? avatar,
        string email,
        Guid parentUserId,
        CancellationToken ct)
    {
        // Se o login já existir (seed reexecutado), não recriia.
        if (await users.GetByEmailAsync(email, ct) is not null)
            return;

        var login = new User(fullName, email, UserRole.Child, birthDate);
        login.SetPasswordHash(hasher.Hash(DefaultPassword));
        await users.AddAsync(login, ct);

        var child = new Child(fullName, birthDate, avatar, parentUserId, login.Id);
        await children.AddAsync(child, ct);

        await unitOfWork.SaveChangesAsync(ct);
    }
}
