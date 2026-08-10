using GerenciadorTasks.Application.Abstractions;
using GerenciadorTasks.Application.Dtos;
using GerenciadorTasks.Core.Entities;
using GerenciadorTasks.Core.Enums;
using GerenciadorTasks.Core.Exceptions;

namespace GerenciadorTasks.Application.Services;

/// <summary>
/// Casos de uso relacionados a crianças.
///
/// O cadastro de uma criança cria, num único fluxo atômico (Unit of Work):
///  - um <see cref="User"/> com <see cref="UserRole.Child"/> (a identidade de login dela);
///  - um <see cref="Child"/> vinculado a esse User e ao responsável logado.
/// Assim a criança passa a poder entrar no app com e-mail + senha e ver só as suas telas.
/// </summary>
public class ChildService
{
    private readonly IChildRepository _children;
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _unitOfWork;

    public ChildService(
        IChildRepository children,
        IUserRepository users,
        IPasswordHasher hasher,
        IUnitOfWork unitOfWork)
    {
        _children = children;
        _users = users;
        _hasher = hasher;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Cadastra uma nova criança vinculada ao responsável informado.
    /// Cria também o login (User{Role=Child}) da criança. Transação atômica.
    /// </summary>
    public async Task<ChildResponse> CreateAsync(
        CreateChildRequest request, Guid parentUserId, CancellationToken ct)
    {
        if (parentUserId == Guid.Empty)
            throw new DomainException("Responsável não autenticado.");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainException("O nome da criança é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new DomainException("O e-mail de acesso da criança é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new DomainException("A senha da criança deve ter ao menos 6 caracteres.");

        var birthDate = DateOnly.Parse(request.BirthDate);
        var email = request.Email.Trim().ToLowerInvariant();

        // E-mail é único — verifica antes para dar erro claro.
        if (await _users.GetByEmailAsync(email, ct) is not null)
            throw new DomainException("Já existe um usuário com este e-mail.");

        // 1. Cria a identidade de login da criança (User com Role=Child).
        var login = new User(request.Name, email, UserRole.Child, birthDate);
        login.SetPasswordHash(_hasher.Hash(request.Password));
        await _users.AddAsync(login, ct);

        // 2. Cria o perfil da criança vinculado ao login e ao responsável.
        var child = new Child(request.Name, birthDate, request.Avatar, parentUserId, login.Id);
        await _children.AddAsync(child, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return ChildResponse.From(child);
    }

    /// <summary>Lista as crianças de um responsável (Role=Parent).</summary>
    public async Task<IReadOnlyList<ChildResponse>> ListByParentAsync(Guid parentUserId, CancellationToken ct)
    {
        var children = await _children.ListByParentAsync(parentUserId, ct);
        return children.Select(ChildResponse.From).ToList();
    }

    /// <summary>Perfil da própria criança logada (buscado pelo seu UserId).</summary>
    public async Task<ChildResponse?> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        var child = await _children.GetByUserIdAsync(userId, ct);
        return child is null ? null : ChildResponse.From(child);
    }

    public async Task<ChildResponse?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var child = await _children.GetByIdAsync(id, ct);
        return child is null ? null : ChildResponse.From(child);
    }

    /// <summary>
    /// Dados completos de uma criança para edição (perfil + e-mail de acesso).
    /// Só o responsável dono pode consultá-lo. Usado pelo formulário de edição.
    /// </summary>
    public async Task<ChildResponse?> GetForEditAsync(Guid childId, Guid parentUserId, CancellationToken ct)
    {
        var child = await _children.GetByIdAsync(childId, ct);
        if (child is null) return null;
        if (child.ParentUserId != parentUserId)
            throw new DomainException("Esta criança não pertence ao seu cadastro.");

        var user = await _users.GetByIdAsync(child.UserId, ct);
        return ChildResponse.From(child, user?.Email ?? string.Empty);
    }

    /// <summary>
    /// Edita uma criança existente (perfil + credenciais de acesso).
    ///
    /// Atualiza, num único fluxo atômico (Unit of Work):
    ///  - o perfil (nome, data de nascimento, avatar) na entidade <see cref="Child"/>;
    ///  - o e-mail e/ou a senha do <see cref="User"/> vinculado (login da criança).
    ///
    /// <paramref name="parentUserId"/> é o responsável logado: só o dono da criança
    /// pode editá-la. A senha é opcional (vazia = manter a atual). O e-mail, se mudou,
    /// é validado quanto à unicidade.
    /// </summary>
    public async Task<ChildResponse?> UpdateAsync(
        Guid childId, UpdateChildRequest request, Guid parentUserId, CancellationToken ct)
    {
        if (parentUserId == Guid.Empty)
            throw new DomainException("Responsável não autenticado.");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainException("O nome da criança é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new DomainException("O e-mail de acesso da criança é obrigatório.");
        if (!string.IsNullOrWhiteSpace(request.Password) && request.Password.Length < 6)
            throw new DomainException("A nova senha deve ter ao menos 6 caracteres.");

        var child = await _children.GetByIdAsync(childId, ct);
        if (child is null) return null;

        // Só o responsável dono da criança pode editá-la.
        if (child.ParentUserId != parentUserId)
            throw new DomainException("Esta criança não pertence ao seu cadastro.");

        var birthDate = DateOnly.Parse(request.BirthDate);
        var newEmail = request.Email.Trim().ToLowerInvariant();

        // 1. Perfil da criança.
        child.UpdateProfile(request.Name, birthDate, request.Avatar);

        // 2. Credenciais do login vinculado (User{Role=Child}).
        var user = await _users.GetByIdAsync(child.UserId, ct)
            ?? throw new DomainException("Login da criança não encontrado.");

        // Mantém o nome do usuário sincronizado com o perfil.
        user.Rename(request.Name);

        // E-mail: só checa unicidade se realmente mudou.
        if (!string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
        {
            if (await _users.GetByEmailAsync(newEmail, ct) is not null)
                throw new DomainException("Já existe um usuário com este e-mail.");
            user.ChangeEmail(newEmail);
        }

        // Senha: só troca se veio preenchida.
        if (!string.IsNullOrWhiteSpace(request.Password))
            user.SetPasswordHash(_hasher.Hash(request.Password));

        await _children.UpdateAsync(child, ct);
        await _users.UpdateAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ChildResponse.From(child);
    }
}
