using GerenciadorTasks.Core.Enums;
using GerenciadorTasks.Core.Exceptions;

namespace GerenciadorTasks.Core.Entities;

public class Reward : BaseEntity
{
    public string Title { get; private set; }
    public string Description { get; private set; }
    public int RequiredPoints { get; private set; }

    public Guid CreatedById { get; private set; }
    public User CreatedBy { get; private set; } = null!;

    public Guid? RedeemedById { get; private set; }
    public User? RedeemedBy { get; private set; }
    public DateTime? RedeemedAt { get; private set; }

    private Reward() { }

    public Reward(string title, string description, int requiredPoints, Guid createdById)
    {
        Validate(title, requiredPoints);

        Title = title;
        Description = description;
        RequiredPoints = requiredPoints;
        CreatedById = createdById;
    }

    public void Redeem(User child)
    {
        if (RedeemedById is not null)
            throw new DomainException("Esta recompensa já foi resgatada.");

        if (child.Points < RequiredPoints)
            throw new DomainException("Pontos insuficientes para resgatar esta recompensa.");

        child.DeductPoints(RequiredPoints);
        RedeemedById = child.Id;
        RedeemedAt = DateTime.UtcNow;
        SetUpdated();
    }

    private static void Validate(string title, int requiredPoints)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Título da recompensa é obrigatório.");

        if (requiredPoints <= 0)
            throw new DomainException("Pontos necessários devem ser maiores que zero.");
    }
}
