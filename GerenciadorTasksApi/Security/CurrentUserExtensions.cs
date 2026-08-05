using System.Security.Claims;

namespace GerenciadorTasksApi.Security;

/// <summary>Extensões para ler dados do usuário autenticado a partir do ClaimsPrincipal.</summary>
public static class CurrentUserExtensions
{
    /// <summary>Id do usuário autenticado, ou null se anônimo (ClaimTypes.NameIdentifier).</summary>
    public static Guid? GetUserId(this ClaimsPrincipal principal)
        => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
