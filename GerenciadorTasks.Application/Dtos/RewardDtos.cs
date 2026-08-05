using GerenciadorTasks.Core.Entities;

namespace GerenciadorTasks.Application.Dtos;

/// <summary>Payload para criar uma recompensa.</summary>
public record CreateRewardRequest(string Title, string Description, int RequiredPoints);

/// <summary>Payload para resgatar uma recompensa (indica qual criança resgata).</summary>
public record RedeemRewardRequest(Guid ChildId);

/// <summary>Recompensa devolvida pela API.</summary>
public record RewardResponse(
    Guid Id,
    string Title,
    string Description,
    int RequiredPoints,
    Guid CreatedById,
    Guid? RedeemedById,
    DateTime? RedeemedAt,
    DateTime CreatedAt)
{
    public static RewardResponse From(Reward r) => new(
        r.Id,
        r.Title,
        r.Description,
        r.RequiredPoints,
        r.CreatedById,
        r.RedeemedById,
        r.RedeemedAt,
        r.CreatedAt);
}
