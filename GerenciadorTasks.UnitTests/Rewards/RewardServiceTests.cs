using GerenciadorTasks.Application.Abstractions;
using GerenciadorTasks.Application.Dtos;
using GerenciadorTasks.Application.Services;
using GerenciadorTasks.Core.Entities;
using GerenciadorTasks.Core.Exceptions;

namespace GerenciadorTasks.UnitTests.Rewards;

/// <summary>
/// Testes do RewardService (criar/listar/resgatar). Fakes isolam a lógica do banco.
/// </summary>
public class RewardServiceTests
{
    private static (RewardService svc, FakeChildRepository children, FakeRewardRepository rewards) NewSut()
    {
        var children = new FakeChildRepository();
        var rewards = new FakeRewardRepository();
        var svc = new RewardService(rewards, children, new FakeUnitOfWork());
        return (svc, children, rewards);
    }

    private static Child MakeChild(int points)
    {
        var c = new Child("João", new DateOnly(2015, 3, 15));
        if (points > 0) c.AddPoints(points);
        return c;
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsReward()
    {
        var (svc, _, _) = NewSut();
        var createdById = Guid.NewGuid();

        var result = await svc.CreateAsync(
            new CreateRewardRequest("Brinde", "Um brinde legal", 80), createdById, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Brinde", result.Title);
        Assert.Equal(80, result.RequiredPoints);
        Assert.Equal(createdById, result.CreatedById);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllCreated()
    {
        var (svc, _, _) = NewSut();
        var uid = Guid.NewGuid();
        await svc.CreateAsync(new CreateRewardRequest("A", "d", 10), uid, CancellationToken.None);
        await svc.CreateAsync(new CreateRewardRequest("B", "d", 20), uid, CancellationToken.None);

        var all = await svc.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task RedeemAsync_WithSufficientPoints_RedeemsAndDeductsPoints()
    {
        var (svc, children, _) = NewSut();
        var uid = Guid.NewGuid();
        var reward = await svc.CreateAsync(new CreateRewardRequest("Prêmio", "d", 50), uid, CancellationToken.None);
        var child = MakeChild(80);
        await children.AddAsync(child, CancellationToken.None);

        var result = await svc.RedeemAsync(reward.Id, child.Id, CancellationToken.None);

        Assert.Equal(child.Id, result.RedeemedById);
        Assert.NotNull(result.RedeemedAt);
        // pontos descontados: 80 - 50 = 30
        var updated = await children.GetByIdAsync(child.Id, CancellationToken.None);
        Assert.Equal(30, updated!.Points);
    }

    [Fact]
    public async Task RedeemAsync_WithInsufficientPoints_ThrowsAndKeepsBalance()
    {
        var (svc, children, _) = NewSut();
        var uid = Guid.NewGuid();
        var reward = await svc.CreateAsync(new CreateRewardRequest("Caro", "d", 200), uid, CancellationToken.None);
        var child = MakeChild(50); // menos que 200
        await children.AddAsync(child, CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() =>
            svc.RedeemAsync(reward.Id, child.Id, CancellationToken.None));

        // saldo intacto (a transação não confirma nada em caso de erro de regra)
        var updated = await children.GetByIdAsync(child.Id, CancellationToken.None);
        Assert.Equal(50, updated!.Points);
    }

    [Fact]
    public async Task RedeemAsync_WithUnknownReward_ThrowsDomainException()
    {
        var (svc, children, _) = NewSut();
        var child = MakeChild(100);
        await children.AddAsync(child, CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() =>
            svc.RedeemAsync(Guid.NewGuid(), child.Id, CancellationToken.None));
    }

    [Fact]
    public async Task RedeemAsync_WithUnknownChild_ThrowsDomainException()
    {
        var (svc, _, _) = NewSut();
        var uid = Guid.NewGuid();
        var reward = await svc.CreateAsync(new CreateRewardRequest("X", "d", 10), uid, CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() =>
            svc.RedeemAsync(reward.Id, Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task RedeemAsync_AlreadyRedeemed_ThrowsDomainException()
    {
        var (svc, children, _) = NewSut();
        var uid = Guid.NewGuid();
        var reward = await svc.CreateAsync(new CreateRewardRequest("Único", "d", 10), uid, CancellationToken.None);
        var child = MakeChild(100);
        await children.AddAsync(child, CancellationToken.None);

        await svc.RedeemAsync(reward.Id, child.Id, CancellationToken.None); // 1º resgate ok

        await Assert.ThrowsAsync<DomainException>(() =>
            svc.RedeemAsync(reward.Id, child.Id, CancellationToken.None)); // 2º falha
    }

    // ---- Fakes ----

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
    }

    private sealed class FakeRewardRepository : IRewardRepository
    {
        private readonly Dictionary<Guid, Reward> _store = new();
        public Task<Reward?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_store.TryGetValue(id, out var r) ? r : null);
        public Task<IReadOnlyList<Reward>> ListAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Reward>>(_store.Values.ToList());
        public Task AddAsync(Reward reward, CancellationToken ct = default) { _store[reward.Id] = reward; return Task.CompletedTask; }
        public Task UpdateAsync(Reward reward, CancellationToken ct = default) { _store[reward.Id] = reward; return Task.CompletedTask; }
    }

    private sealed class FakeChildRepository : IChildRepository
    {
        private readonly Dictionary<Guid, Child> _store = new();
        public Task<Child?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_store.TryGetValue(id, out var c) ? c : null);
        public Task<IReadOnlyList<Child>> ListAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Child>>(_store.Values.ToList());
        public Task AddAsync(Child child, CancellationToken ct = default) { _store[child.Id] = child; return Task.CompletedTask; }
        public Task UpdateAsync(Child child, CancellationToken ct = default) { _store[child.Id] = child; return Task.CompletedTask; }
    }
}
