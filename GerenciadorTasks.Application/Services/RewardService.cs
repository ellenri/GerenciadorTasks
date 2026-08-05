using GerenciadorTasks.Application.Abstractions;
using GerenciadorTasks.Application.Dtos;
using GerenciadorTasks.Core.Entities;
using GerenciadorTasks.Core.Exceptions;

namespace GerenciadorTasks.Application.Services;

/// <summary>
/// Casos de uso de recompensas: criar, listar e resgatar.
///
/// O resgate coordena DOIS agregados (Reward + Child) numa única transação (UoW):
/// <c>Reward.Redeem(child)</c> valida a regra (já resgatada? saldo suficiente?) e
/// desconta os pontos da criança; o serviço só confirma tudo de uma vez.
/// </summary>
public class RewardService
{
    private readonly IRewardRepository _rewards;
    private readonly IChildRepository _children;
    private readonly IUnitOfWork _unitOfWork;

    public RewardService(IRewardRepository rewards, IChildRepository children, IUnitOfWork unitOfWork)
    {
        _rewards = rewards;
        _children = children;
        _unitOfWork = unitOfWork;
    }

    public async Task<RewardResponse> CreateAsync(CreateRewardRequest request, Guid createdById, CancellationToken ct)
    {
        var reward = new Reward(request.Title, request.Description, request.RequiredPoints, createdById);
        await _rewards.AddAsync(reward, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return RewardResponse.From(reward);
    }

    public async Task<IReadOnlyList<RewardResponse>> GetAllAsync(CancellationToken ct)
    {
        var rewards = await _rewards.ListAsync(ct);
        return rewards.Select(RewardResponse.From).ToList();
    }

    /// <summary>Resgata a recompensa para uma criança (desconta pontos). Transação atômica.</summary>
    public async Task<RewardResponse> RedeemAsync(Guid rewardId, Guid childId, CancellationToken ct)
    {
        var reward = await _rewards.GetByIdAsync(rewardId, ct)
            ?? throw new DomainException("Recompensa não encontrada.");

        var child = await _children.GetByIdAsync(childId, ct)
            ?? throw new DomainException("Criança não encontrada.");

        reward.Redeem(child); // valida "já resgatada" e saldo (lança DomainException)

        await _rewards.UpdateAsync(reward, ct);
        await _children.UpdateAsync(child, ct);
        await _unitOfWork.SaveChangesAsync(ct); // atômico: ou ambos salvam, ou nada

        return RewardResponse.From(reward);
    }
}
