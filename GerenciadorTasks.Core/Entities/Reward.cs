using GerenciadorTasks.Core.Exceptions;

namespace GerenciadorTasks.Core.Entities;

/// <summary>
/// Recompensa resgatável com pontos acumulados pelas crianças (gamificação).
///
/// Pertence a um responsável (CreatedById) e é "comprada" por uma <see cref="Child"/>.
/// O desconto dos pontos fica no agregado Child (DeductPoints); aqui só marcamos
/// o resgate — coerente com o padrão das outras entidades (apenas Guids, sem
/// navigation properties no domínio).
/// </summary>
public class Reward : BaseEntity
{
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public int RequiredPoints { get; private set; }

    public Guid CreatedById { get; private set; }
    public Guid? RedeemedById { get; private set; }
    public DateTime? RedeemedAt { get; private set; }

    private Reward() { }

    public Reward(string title, string description, int requiredPoints, Guid createdById)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Título da recompensa é obrigatório.");
        if (requiredPoints <= 0)
            throw new DomainException("Pontos necessários devem ser maiores que zero.");
        if (createdById == Guid.Empty)
            throw new DomainException("A recompensa precisa ter um responsável.");

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim();
        RequiredPoints = requiredPoints;
        CreatedById = createdById;
    }

    /// <summary>
    /// Resgata a recompensa: desconta os pontos da criança e marca como resgatada.
    /// Recebe a <see cref="Child"/> (que acumula pontos), não o User.
    /// Se a criança não tiver pontos suficientes, Child.DeductPoints lança
    /// DomainException e nada aqui é alterado (consistência preservada).
    /// </summary>
    public void Redeem(Child child)
    {
        if (RedeemedById is not null)
            throw new DomainException("Esta recompensa já foi resgatada.");
        if (child is null)
            throw new DomainException("Criança inválida.");

        child.DeductPoints(RequiredPoints); // valida saldo (lança DomainException se insuficiente)

        RedeemedById = child.Id;
        RedeemedAt = DateTime.UtcNow;
        Touch();
    }
}
