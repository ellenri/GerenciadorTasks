namespace GerenciadorTasks.Core.Enums;

/// <summary>
/// Padrão de repetição de uma missão ao ser cadastrada.
/// Once = única vez (uma ocorrência); Weekly = repete 1 dia por semana;
/// TwiceWeekly = repete 2 dias por semana. As ocorrências são geradas como
/// instâncias independentes no momento do cadastro (horizonte de 8 semanas).
/// </summary>
public enum RecurrenceType
{
    Once = 0,
    Weekly = 1,
    TwiceWeekly = 2
}
