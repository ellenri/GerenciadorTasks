namespace GerenciadorTasks.Application.Abstractions;

/// <summary>
/// Abstrai o hash e a verificação de senhas.
/// A implementação concreta (BCrypt, Argon2, etc.) vive em Infrastructure —
/// o Application só conhece o contrato (inversão de dependência).
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Gera o hash de uma senha em texto plano. Nunca armazena o texto.</summary>
    string Hash(string password);

    /// <summary>Verifica se a senha em texto plano bate com o hash armazenado.</summary>
    bool Verify(string password, string passwordHash);
}
