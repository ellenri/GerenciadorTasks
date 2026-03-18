using GerenciadorTasks.Core.Enums;
using GerenciadorTasks.Core.Exceptions;

namespace GerenciadorTasks.Core.Entities;

public class User : BaseEntity
{
    private readonly List<TaskItem> _createdTasks = [];
    private readonly List<TaskItem> _assignedTasks = [];

    public string FullName { get; private set; }
    public DateTime BirthDate { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public string? AvatarUrl { get; private set; }
    public int Points { get; private set; }

    public Guid? ParentId { get; private set; }
    public User? Parent { get; private set; }
    public ICollection<User> Children { get; private set; } = [];
    public ICollection<TaskItem> CreatedTasks => _createdTasks;
    public ICollection<TaskItem> AssignedTasks => _assignedTasks;

    private User() { }

    public User(string fullName, DateTime birthDate, string email, string passwordHash, UserRole role, Guid? parentId = null)
    {
        Validate(fullName, birthDate, email, passwordHash);

        FullName = fullName;
        BirthDate = birthDate;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        ParentId = parentId;
        Points = 0;
    }

    public void UpdateProfile(string fullName, string? avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("Nome completo é obrigatório.");

        FullName = fullName;
        AvatarUrl = avatarUrl;
        SetUpdated();
    }

    public void UpdatePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("Hash de senha é obrigatório.");

        PasswordHash = newPasswordHash;
        SetUpdated();
    }

    public void AddPoints(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Pontos a adicionar devem ser maiores que zero.");

        Points += amount;
        SetUpdated();
    }

    public void DeductPoints(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Pontos a deduzir devem ser maiores que zero.");
        if (Points < amount)
            throw new DomainException("Pontos insuficientes.");

        Points -= amount;
        SetUpdated();
    }

    private static void Validate(string fullName, DateTime birthDate, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("Nome completo é obrigatório.");

        if (birthDate > DateTime.UtcNow)
            throw new DomainException("Data de nascimento inválida.");

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email é obrigatório.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Senha é obrigatória.");
    }
}
