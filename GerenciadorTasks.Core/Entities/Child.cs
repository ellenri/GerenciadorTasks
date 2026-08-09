using GerenciadorTasks.Core.Exceptions;

namespace GerenciadorTasks.Core.Entities;

/// <summary>
/// Uma criança que recebe missões e acumula pontos (gamificação).
///
/// Vincula-se a um responsável (<see cref="ParentUserId"/>, User com Role=Parent)
/// e a uma identidade de acesso (<see cref="UserId"/>, User com Role=Child) usada
/// no login da própria criança.
/// </summary>
public class Child : BaseEntity
{
    // null!: o construtor público SEMPRE inicializa; o privado só é usado pelo EF Core
    // (que preenche via reflexão). O operador '!' silencia o aviso CS8618 com segurança.
    public string FullName { get; private set; } = null!;
    public DateOnly BirthDate { get; private set; }
    public string? AvatarPath { get; private set; }
    public int Points { get; private set; }

    /// <summary>Responsável (User com Role=Parent) que cadastrou a criança.</summary>
    public Guid ParentUserId { get; private set; }

    /// <summary>Identidade de login da criança (User com Role=Child).</summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Construtor de compatibilidade (sem vínculos). Mantido para testes de domínio
    /// e materialização do EF Core. O fluxo de produção usa a sobrecarga com vínculos.
    /// </summary>
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

    /// <summary>
    /// Construtor de produção: cria a criança já vinculada ao responsável e à
    /// identidade de login dela. Estes vínculos são obrigatórios no cadastro real.
    /// </summary>
    public Child(
        string fullName,
        DateOnly birthDate,
        string? avatarPath,
        Guid parentUserId,
        Guid userId)
        : this(fullName, birthDate, avatarPath)
    {
        if (parentUserId == Guid.Empty)
            throw new DomainException("A criança precisa estar vinculada a um responsável.");
        if (userId == Guid.Empty)
            throw new DomainException("A criança precisa ter um usuário de acesso (login).");

        ParentUserId = parentUserId;
        UserId = userId;
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

    /// <summary>
    /// Desconta pontos ao resgatar uma recompensa. Não permite ficar negativo.
    /// Usado por <see cref="Reward.Redeem"/>. Lança DomainException se o saldo
    /// for insuficiente — quem chama deve tratar/propagar como erro de regra.
    /// </summary>
    public void DeductPoints(int points)
    {
        if (points < 0)
            throw new DomainException("A quantidade de pontos não pode ser negativa.");
        if (points > Points)
            throw new DomainException("Pontos insuficientes para resgatar a recompensa.");

        Points -= points;
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
