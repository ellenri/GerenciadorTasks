using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GerenciadorTasks.Infrastructure.Persistence;

namespace GerenciadorTasksApi.Controllers;

/// <summary>
/// Endpoint de verificação de saúde da aplicação.
/// Responde OK apenas quando o backend está vivo E o banco de dados
/// (do qual todos os serviços dependem) está acessível.
/// </summary>
[ApiController]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /health — verifica se o backend está saudável.
    ///
    /// Testa a conectividade com o banco de dados, que é a dependência
    /// compartilhada por todos os serviços (Task, User, Child, Reward,
    /// Notification, Reminder). Se o banco responde, os serviços respondem.
    /// </summary>
    [HttpGet("/health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Health(CancellationToken ct)
    {
        try
        {
            var canConnect = await _db.Database.CanConnectAsync(ct);
            if (!canConnect)
                return StatusCode(503, new { status = "UNHEALTHY", database = "unreachable" });

            return Ok(new { status = "OK", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { status = "UNHEALTHY", error = ex.Message });
        }
    }
}
