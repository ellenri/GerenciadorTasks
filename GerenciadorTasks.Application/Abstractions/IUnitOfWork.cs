namespace GerenciadorTasks.Application.Abstractions;

/// <summary>
/// Unit of Work: confirma em BLOCO todas as mudanças feitas nos repositórios.
///
/// GANCHO com o Desafio 2: é isto que garante a transação ATÔMICA entre agregados.
/// Sem ele, "concluir uma missão" poderia salvar a tarefa mas falhar ao creditar
/// os pontos (ou vice-versa), deixando o sistema inconsistente.
/// Com IUnitOfWork, ou tudo salva, ou nada salva.
/// </summary>
public interface IUnitOfWork
{
    /// <returns>Número de entidades gravadas no banco.</returns>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
