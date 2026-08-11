using GerenciadorTasks.Application.Services;
using Microsoft.Extensions.Hosting;

namespace GerenciadorTasksApi.BackgroundServices;

/// <summary>
/// Agendador de lembretes em background.
///
/// As notificações comuns são reativas (criadas num evento). Lembrete é diferente:
/// precisa disparar num HORÁRIO. Sem um broker de mensagens, este IHostedService
/// roda um Timer de 1 em 1 minuto, cria um escopo de DI e delega ao
/// <see cref="ReminderService"/> o disparo das notificações devidas ("avisar X min
/// antes" e "na hora"). A idempotência vive no domínio (ReminderBeforeSentAt /
/// ReminderAtStartSentAt), então ticks repetidos nunca geram notificação duplicada.
/// </summary>
public sealed class ReminderHostedService : IHostedService, IDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceProvider _services;
    private readonly ILogger<ReminderHostedService> _logger;
    private Timer? _timer;

    public ReminderHostedService(IServiceProvider services, ILogger<ReminderHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Agendador de lembretes iniciado (intervalo de {Interval}s).", Interval.TotalSeconds);

        // dueTime Zero: dispara já na partida (pega lembretes atrasados se o server ficou fora).
        // callback síncrono que descarta a Task; o tratamento de erro está em RunAsync.
        _timer = new Timer(
            callback: _ => _ = RunAsync(),
            state: null,
            dueTime: TimeSpan.Zero,
            period: Interval);

        return Task.CompletedTask;
    }

    private async Task RunAsync()
    {
        try
        {
            // Escopo por tick: o ReminderService (e seus repositórios/UoW) são Scoped.
            using var scope = _services.CreateScope();
            var reminders = scope.ServiceProvider.GetRequiredService<ReminderService>();
            await reminders.ProcessDueRemindersAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            // O timer não pode derrubar o processo: capturamos e logamos tudo.
            _logger.LogError(ex, "Erro ao processar lembretes agendados.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        _logger.LogInformation("Agendador de lembretes parado.");
        return Task.CompletedTask;
    }

    public void Dispose() => _timer?.Dispose();
}
