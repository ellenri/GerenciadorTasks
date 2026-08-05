namespace GerenciadorTasks.Core.Exceptions;

/// <summary>
/// Lançada quando uma regra de negócio (invariante do domínio) é violada.
/// Exemplos: criar criança sem nome, concluir missão já concluída.
///
/// Por que uma exceção própria? Para separar "erro de programação/infra"
/// (NullReferenceException, falha de BD) de "regra de negócio quebrada".
/// O chamador pode capturar DomainException e traduzi-la em HTTP 400, por exemplo.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}
