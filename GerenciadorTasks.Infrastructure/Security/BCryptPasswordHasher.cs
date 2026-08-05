using GerenciadorTasks.Application.Abstractions;

namespace GerenciadorTasks.Infrastructure.Security;

/// <summary>
/// Implementação de <see cref="IPasswordHasher"/> com BCrypt.Net-Next.
/// BCrypt é lento de propósito (custo ajustável) — é o que dificulta a quebra
/// do hash caso o banco vaze. O WorkFactor 11 é um bom equilíbrio custo/segurança.
/// </summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string passwordHash)
        => BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
