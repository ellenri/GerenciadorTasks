using GerenciadorTasks.Core.Entities;

namespace GerenciadorTasks.Application.Dtos;

/// <summary>Payload de uma criança para a API (leitura).</summary>
public record ChildResponse(
    Guid Id,
    string Name,
    string? Avatar,
    string? BirthDate,  // "YYYY-MM-DD"
    int Points,
    DateTime CreatedAt)
{
    public static ChildResponse From(Child c) => new(
        Id: c.Id,
        Name: c.FullName,
        Avatar: c.AvatarPath,
        BirthDate: c.BirthDate.ToString("yyyy-MM-dd"),
        Points: c.Points,
        CreatedAt: c.CreatedAt);
}

/// <summary>
/// Payload para CADASTRAR uma nova criança.
/// Espelha o que o formulário do Astro envia.
/// </summary>
public record CreateChildRequest(
    string Name,
    string BirthDate,  // "YYYY-MM-DD"
    string? Avatar);   // caminho do avatar, ex.: "/avatars/boy1.png"
