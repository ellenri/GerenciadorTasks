namespace GerenciadorTasks.Core.Entities;

/// <summary>
/// Base de todas as entidades do domínio.
/// Centraliza identidade (Id) e auditoria (datas de criação/atualização).
///
/// "abstract" porque não faz sentido instanciar uma entidade genérica —
/// apenas suas especializações (Child, TaskItem, User).
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Construtor sem parâmetros usado pelo EF Core ao ler do banco (materialização).
    // protected deixa as subclasses chamarem, mas impede uso externo direto.
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Marca a entidade como atualizada. Toda mutação deve chamar Touch()
    /// para manter UpdatedAt correto (Single Source of Truth para auditoria).
    /// </summary>
    protected void Touch() => UpdatedAt = DateTime.UtcNow;
}
