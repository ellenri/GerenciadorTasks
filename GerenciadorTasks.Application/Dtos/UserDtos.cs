using GerenciadorTasks.Core.Entities;

namespace GerenciadorTasks.Application.Dtos;

/// <summary>Payload para cadastrar um responsável (Parent).</summary>
public record RegisterRequest(string FullName, string Email, string Password);

/// <summary>Payload de login.</summary>
public record LoginRequest(string Email, string Password);

/// <summary>Dados do usuário autenticado devolvidos pela API.</summary>
public record AuthUserResponse(Guid Id, string FullName, string Email, string Role)
{
    public static AuthUserResponse From(User u) => new(u.Id, u.FullName, u.Email, u.Role.ToString());
}
