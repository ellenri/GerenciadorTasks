using GerenciadorTasks.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GerenciadorTasksApi.ExceptionHandling;

/// <summary>
/// Converte DomainException (regra de negócio quebrada) em HTTP 400 automaticamente.
///
/// GANCHO com a revisão: lembra do "Destaque 6" (DomainException separada)?
/// É AQUI que a separação se paga. Todo controller confia que regras inválidas
/// viram 400 sem precisar de try/catch repetido — tratamento centralizado (DRY).
/// </summary>
public class DomainExceptionHandler : IExceptionHandler
{
    private readonly ILogger<DomainExceptionHandler> _logger;

    public DomainExceptionHandler(ILogger<DomainExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Se NÃO for DomainException, retorna false = "deixa outro handler decidir".
        if (exception is not DomainException domainException)
            return false;

        // Regra de negócio quebrada é esperada (erro do cliente), não um bug -> Warning.
        _logger.LogWarning("Regra de negócio violada: {Message}", domainException.Message);

        // ProblemDetails é o formato padronizado (RFC 7807) de erro de API.
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Regra de negócio",
            Detail = domainException.Message,
            Instance = httpContext.Request.Path
        }, cancellationToken);

        return true; // tratado: o pipeline para aqui.
    }
}
