using System.Security.Claims;
using GerenciadorTasks.Application.Dtos;
using GerenciadorTasks.Application.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GerenciadorTasksApi.Controllers;

/// <summary>
/// Autenticação por cookie HttpOnly.
/// register/login chamam SignInAsync (emite o cookie); logout chama SignOutAsync.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    public const string AuthScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    private readonly UserService _users;

    public AuthController(UserService users) => _users = users;

    /// POST /api/auth/register — cadastra um responsável e já o autentica (200).
    [HttpPost("register")]
    public async Task<ActionResult<AuthUserResponse>> Register(
        [FromBody] RegisterRequest request, CancellationToken ct)
    {
        var user = await _users.RegisterAsync(request, ct);
        await SignInAsync(user);
        return Ok(user);
    }

    /// POST /api/auth/login — autentica um responsável existente (200) ou 401.
    [HttpPost("login")]
    public async Task<ActionResult<AuthUserResponse>> Login(
        [FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await _users.LoginAsync(request, ct);
        if (user is null)
            return Unauthorized(new { message = "E-mail ou senha inválidos." });

        await SignInAsync(user);
        return Ok(user);
    }

    /// POST /api/auth/logout — encerra a sessão (204).
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AuthScheme);
        return NoContent();
    }

    /// GET /api/auth/me — quem está autenticado (401 se anônimo).
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me() => Ok(new
    {
        id = User.FindFirstValue(ClaimTypes.NameIdentifier),
        name = User.FindFirstValue(ClaimTypes.Name),
        email = User.FindFirstValue(ClaimTypes.Email),
        role = User.FindFirstValue(ClaimTypes.Role)
    });

    private async Task SignInAsync(AuthUserResponse user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, AuthScheme);
        await HttpContext.SignInAsync(AuthScheme, new ClaimsPrincipal(identity));
    }
}
