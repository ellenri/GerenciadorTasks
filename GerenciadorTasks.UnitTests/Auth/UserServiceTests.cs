using GerenciadorTasks.Application.Abstractions;
using GerenciadorTasks.Application.Dtos;
using GerenciadorTasks.Application.Services;
using GerenciadorTasks.Core.Entities;
using GerenciadorTasks.Core.Exceptions;

namespace GerenciadorTasks.UnitTests.Auth;

/// <summary>
/// Testes do UserService (cadastro/login). Usa fakes de repositório/hasher/uow
/// para isolar a lógica de aplicação do banco e do BCrypt.
/// </summary>
public class UserServiceTests
{
    private static UserService NewSut()
        => new(new FakeUserRepository(), new FakePasswordHasher(), new FakeUnitOfWork());

    private static RegisterRequest ValidRegister(string? email = null, string? password = null)
        => new("Responsável Teste", email ?? "teste@exemplo.com", password ?? "senha123");

    [Fact]
    public async Task RegisterAsync_WithValidData_ReturnsUser()
    {
        var sut = NewSut();

        var result = await sut.RegisterAsync(ValidRegister(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Responsável Teste", result.FullName);
        Assert.Equal("teste@exemplo.com", result.Email);
        Assert.Equal("Parent", result.Role);
    }

    [Fact]
    public async Task RegisterAsync_NormalizesEmailToLowerCase()
    {
        var repo = new FakeUserRepository();
        var sut = new UserService(repo, new FakePasswordHasher(), new FakeUnitOfWork());

        await sut.RegisterAsync(ValidRegister(email: "TESTE@Exemplo.COM"), CancellationToken.None);

        var stored = Assert.Single(repo.Store.Values);
        Assert.Equal("teste@exemplo.com", stored.Email);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ThrowsDomainException()
    {
        var sut = NewSut();
        await sut.RegisterAsync(ValidRegister(), CancellationToken.None);

        var act = () => sut.RegisterAsync(ValidRegister(), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<DomainException>(act);
        Assert.Contains("e-mail", ex.Message.ToLower());
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")] // abaixo do mínimo (6)
    public async Task RegisterAsync_WithShortPassword_ThrowsDomainException(string password)
    {
        var sut = NewSut();

        var act = () => sut.RegisterAsync(ValidRegister(password: password), CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(act);
    }

    [Fact]
    public async Task LoginAsync_WithCorrectCredentials_ReturnsUser()
    {
        var sut = NewSut();
        await sut.RegisterAsync(ValidRegister(email: "a@b.com", password: "senha123"), CancellationToken.None);

        var result = await sut.LoginAsync(new LoginRequest("a@b.com", "senha123"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("a@b.com", result!.Email);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsNull()
    {
        var sut = NewSut();
        await sut.RegisterAsync(ValidRegister(email: "a@b.com", password: "senha123"), CancellationToken.None);

        var result = await sut.LoginAsync(new LoginRequest("a@b.com", "errada"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ReturnsNull()
    {
        var sut = NewSut();

        var result = await sut.LoginAsync(new LoginRequest("ninguem@b.com", "qualquer"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_IsCaseInsensitiveForEmail()
    {
        var sut = NewSut();
        await sut.RegisterAsync(ValidRegister(email: "a@b.com", password: "senha123"), CancellationToken.None);

        var result = await sut.LoginAsync(new LoginRequest("A@B.COM", "senha123"), CancellationToken.None);

        Assert.NotNull(result);
    }

    // ---- Fakes (substituem banco + BCrypt nos testes) ----

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => "hash:" + password;
        public bool Verify(string password, string hash) => hash == "hash:" + password;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
    }

    internal sealed class FakeUserRepository : IUserRepository
    {
        public readonly Dictionary<Guid, User> Store = new();

        public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Store.TryGetValue(id, out var u) ? u : null);

        public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
            => Task.FromResult(Store.Values.FirstOrDefault(u => u.Email == email));

        public Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<User>>(Store.Values.ToList());

        public Task AddAsync(User user, CancellationToken ct = default)
        {
            Store[user.Id] = user;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken ct = default)
        {
            Store[user.Id] = user;
            return Task.CompletedTask;
        }
    }
}
