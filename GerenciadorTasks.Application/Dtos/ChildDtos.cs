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
    string? Email,      // e-mail de acesso (só preenchido na edição)
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
        Email: null,
        CreatedAt: c.CreatedAt);

    /// Sobrecarga usada na edição: inclui o e-mail de acesso do User vinculado.
    public static ChildResponse From(Child c, string email) => new(
        Id: c.Id,
        Name: c.FullName,
        Avatar: c.AvatarPath,
        BirthDate: c.BirthDate.ToString("yyyy-MM-dd"),
        Points: c.Points,
        ParentUserId: c.ParentUserId,
        UserId: c.UserId,
        Email: email,
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

/// <summary>
/// Payload para EDITAR uma criança existente.
/// A senha é opcional: se vier vazia/nula, mantém a senha atual.
/// </summary>
public record UpdateChildRequest(
    string Name,
    string BirthDate,    // "YYYY-MM-DD"
    string? Avatar,
    string Email,        // novo e-mail de acesso (único)
    string? Password);   // nova senha (mín. 6); vazio = manter a atual
