using System.Security.Claims;
using GerenciadorTasks.Core.Enums;

namespace GerenciadorTasksApi.Security;

/// <summary>Extensões para ler dados do usuário autenticado a partir do ClaimsPrincipal.</summary>
public static class CurrentUserExtensions
{
    /// <summary>Id do usuário autenticado, ou null se anônimo (ClaimTypes.NameIdentifier).</summary>
    public static Guid? GetUserId(this ClaimsPrincipal principal)
        => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>Papel (Parent/Child) do usuário autenticado, ou null se anônimo.</summary>
    public static UserRole? GetRole(this ClaimsPrincipal principal)
        => Enum.TryParse<UserRole>(principal.FindFirstValue(ClaimTypes.Role), out var role) ? role : null;

    public static bool IsParent(this ClaimsPrincipal principal) => principal.GetRole() == UserRole.Parent;
    public static bool IsChild(this ClaimsPrincipal principal) => principal.GetRole() == UserRole.Child;
}
