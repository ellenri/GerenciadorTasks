using GerenciadorTasks.Application.Abstractions;
using GerenciadorTasks.Application.Dtos;
using GerenciadorTasks.Core.Entities;
using GerenciadorTasks.Core.Enums;
using GerenciadorTasks.Core.Exceptions;

namespace GerenciadorTasks.Application.Services;

/// <summary>
/// Casos de uso de autenticação: cadastro e login de responsáveis (Parent).
/// Não conhece o mecanismo de hash (delega a <see cref="IPasswordHasher"/>) nem o
/// de sessão (o controller é quem emite o cookie). Aqui só orquestramos regras.
/// </summary>
public class UserService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUserRepository users, IPasswordHasher hasher, IUnitOfWork unitOfWork)
    {
        _users = users;
        _hasher = hasher;
        _unitOfWork = unitOfWork;
    }

    /// <summary>Cadastra um novo responsável. Lança DomainException em validações.</summary>
    public async Task<AuthUserResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new DomainException("O nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new DomainException("O e-mail é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new DomainException("A senha deve ter ao menos 6 caracteres.");

        var email = request.Email.Trim().ToLowerInvariant();

        // E-mail é único — verifica antes para dar erro claro (não depender do banco).
        if (await _users.GetByEmailAsync(email, ct) is not null)
            throw new DomainException("Já existe um usuário com este e-mail.");

        var user = new User(request.FullName, email, UserRole.Parent);
        user.SetPasswordHash(_hasher.Hash(request.Password));

        await _users.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return AuthUserResponse.From(user);
    }

    /// <summary>
    /// Autentica por e-mail + senha.
    /// Devolve o usuário ou null — nunca distingue "e-mail não existe" de
    /// "senha errada" (evita enumeração de contas).
    /// </summary>
    public async Task<AuthUserResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(email, ct);
        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
            return null;

        if (!_hasher.Verify(request.Password ?? string.Empty, user.PasswordHash))
            return null;

        return AuthUserResponse.From(user);
    }
}
