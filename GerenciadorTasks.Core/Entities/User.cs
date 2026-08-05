using GerenciadorTasks.Core.Enums;
using GerenciadorTasks.Core.Exceptions;

namespace GerenciadorTasks.Core.Entities;

/// <summary>
/// Usuário do sistema: um responsável (Parent) que cria missões para as crianças.
///
/// Refatoração: a versão original guardava a senha em texto plano (vulnerabilidade
/// grave). Aqui guardamos apenas o HASH — o cálculo do hash (BCrypt, etc.) é
/// responsabilidade da camada de Application/Infrastructure, não do domínio.
/// </summary>
public class User : BaseEntity
{
    public string FullName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public DateOnly? BirthDate { get; private set; }
    public UserRole Role { get; private set; }
    public string? PasswordHash { get; private set; }

    private User() : base() { }

    public User(string fullName, string email, UserRole role, DateOnly? birthDate = null)
        : base()
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("O nome do usuário é obrigatório.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("O e-mail do usuário é obrigatório.");

        FullName = fullName.Trim();
        // Normaliza o e-mail: sempre minúsculo, sem espaços. Evita duplicidade por casing.
        Email = email.Trim().ToLowerInvariant();
        Role = role;
        BirthDate = birthDate;
    }

    /// Define o hash da senha (nunca a senha em texto plano).
    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("O hash da senha é obrigatório.");

        PasswordHash = passwordHash;
        Touch();
    }
}
