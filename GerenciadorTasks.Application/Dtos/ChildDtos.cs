using GerenciadorTasks.Core.Entities;

namespace GerenciadorTasks.Application.Dtos;

/// <summary>Payload de uma criança para a API (leitura).</summary>
public record ChildResponse(
    Guid Id,
    string Name,
    string? Avatar,
    string? BirthDate,  // "YYYY-MM-DD"
    int Points,
    Guid ParentUserId,  // responsável (User Parent)
    Guid UserId,        // login da criança (User Child)
    DateTime CreatedAt)
{
    public static ChildResponse From(Child c) => new(
        Id: c.Id,
        Name: c.FullName,
        Avatar: c.AvatarPath,
        BirthDate: c.BirthDate.ToString("yyyy-MM-dd"),
        Points: c.Points,
        ParentUserId: c.ParentUserId,
        UserId: c.UserId,
        CreatedAt: c.CreatedAt);
}

/// <summary>
/// Payload para CADASTRAR uma nova criança.
/// Além dos dados de perfil, inclui as credenciais de acesso (e-mail + senha)
/// que a criança usará para entrar no app.
/// </summary>
public record CreateChildRequest(
    string Name,
    string BirthDate,  // "YYYY-MM-DD"
    string? Avatar,    // caminho do avatar, ex.: "/avatars/boy1.png"
    string Email,      // login da criança (único)
    string Password);  // senha da criança (mín. 6)
