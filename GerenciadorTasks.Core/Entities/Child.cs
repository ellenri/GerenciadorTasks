using GerenciadorTasks.Core.Exceptions;

namespace GerenciadorTasks.Core.Entities;

/// <summary>
/// Uma criança que recebe missões e acumula pontos (gamificação).
/// </summary>
public class Child : BaseEntity
{
    // null!: o construtor público SEMPRE inicializa; o privado só é usado pelo EF Core
    // (que preenche via reflexão). O operador '!' silencia o aviso CS8618 com segurança.
    public string FullName { get; private set; } = null!;
    public DateOnly BirthDate { get; private set; }
    public string? AvatarPath { get; private set; }
    public int Points { get; private set; }

    public Child(string fullName, DateOnly birthDate, string? avatarPath = null)
        : base()
    {
        // Invariantes: o objeto DEVE nascer válido. Se falhar, não há objeto inválido solto pelo sistema.
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("O nome da criança é obrigatório.");

        if (birthDate > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new DomainException("A data de nascimento não pode estar no futuro.");

        FullName = fullName.Trim();
        BirthDate = birthDate;
        AvatarPath = avatarPath;
        Points = 0;
    }

    /// Idade calculada (propriedade somente-leitura derivada — não é armazenada no banco).
    public int Age
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            int age = today.Year - BirthDate.Year;
            if (BirthDate > today.AddYears(-age)) age--;
            return age;
        }
    }

    /// Creditar pontos de recompensa. Gamificação: positivo apenas.
    public void AddPoints(int points)
    {
        if (points < 0)
            throw new DomainException("A quantidade de pontos não pode ser negativa.");
        Points += points;
        Touch();
    }

    public void Rename(string newFullName)
    {
        if (string.IsNullOrWhiteSpace(newFullName))
            throw new DomainException("O nome da criança é obrigatório.");
        FullName = newFullName.Trim();
        Touch();
    }

    public void SetAvatar(string? avatarPath)
    {
        AvatarPath = avatarPath;
        Touch();
    }
}
