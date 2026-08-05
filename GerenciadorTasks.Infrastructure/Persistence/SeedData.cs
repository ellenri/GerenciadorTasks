using GerenciadorTasks.Application.Abstractions;
using GerenciadorTasks.Core.Entities;

namespace GerenciadorTasks.Infrastructure.Persistence;

/// <summary>
/// Dados iniciais (seed) e constantes do ambiente.
///
/// Como ainda não temos autenticação, existe um "responsável padrão" fixo.
/// (TODO futuro: trocar por login real e remover o Guid fixo.)
/// </summary>
public static class SeedData
{
    /// Id fixo do responsável padrão (placeholder até existir auth).
    public static readonly Guid DefaultParentId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// Popula as crianças iniciais se o repositório estiver vazio.
    /// Idempotente: pode ser chamado várias vezes sem duplicar.
    public static async Task InitializeAsync(IChildRepository children, CancellationToken ct = default)
    {
        if ((await children.ListAsync(ct)).Count > 0)
            return; // já populado

        await children.AddAsync(new Child("João Silva", new DateOnly(2015, 3, 15), "/avatars/boy1.png"), ct);
        await children.AddAsync(new Child("Maria Silva", new DateOnly(2017, 7, 22), "/avatars/girl_blondehair.png"), ct);
        await children.AddAsync(new Child("Pedro Silva", new DateOnly(2019, 11, 8), "/avatars/boy_cap.png"), ct);
    }
}
