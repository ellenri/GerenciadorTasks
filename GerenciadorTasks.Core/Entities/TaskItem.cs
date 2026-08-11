using GerenciadorTasks.Core.Enums;
using GerenciadorTasks.Core.Exceptions;

// Alias: desambigua de System.Threading.Tasks.TaskStatus (que também existe no .NET).
using TaskStatus = GerenciadorTasks.Core.Enums.TaskStatus;

namespace GerenciadorTasks.Core.Entities;

/// <summary>
/// Uma "missão" (tarefa) atribuída a uma criança por um responsável.
///
/// Chama-se TaskItem (e não Task) porque "Task" já é um tipo fundamental do .NET
/// (System.Threading.Tasks.Task). Reutilizar o nome causaria confusão e bugs.
/// </summary>
public class TaskItem : BaseEntity
{
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public TaskCategory Category { get; private set; }
    public TaskPriority Priority { get; private set; }
    public TaskStatus Status { get; private set; }
    public DateOnly ScheduledDate { get; private set; }
    public TimeOnly ScheduledTime { get; private set; }
    public int? EstimatedDurationMinutes { get; private set; }

    public Guid AssignedToId { get; private set; }   // FK -> Child
    public Guid CreatedById { get; private set; }     // FK -> User (responsável)

    public DateTime? CompletedAt { get; private set; }

    // Fluxo de aprovação com comprovação por imagem:
    /// <summary>Foto enviada pela criança como comprovação (URL relativa, ex.: "/uploads/abc.jpg").</summary>
    public string? SubmissionImageUrl { get; private set; }
    /// <summary>Quando a criança enviou a comprovação.</summary>
    public DateTime? SubmittedAt { get; private set; }
    /// <summary>Feedback do responsável ao rejeitar (ex.: "falta limpar a pia").</summary>
    public string? ReviewerComment { get; private set; }

    /// <summary>
    /// Identificador que agrupa as ocorrências de uma mesma missão recorrente
    /// (null para missão única). Não afeta o estado — só registra o vínculo.
    /// </summary>
    public Guid? RecurrenceGroupId { get; private set; }

    // ====================== Lembretes (lembrete antes / na hora) ======================
    // Configuração (definida pelo responsável ao criar a missão) + estado de envio
    // (controlado pelo ReminderService — os timestamps garantem idempotência:
    // o agendador nunca reenvia o mesmo lembrete duas vezes).
    /// <summary>Se verdadeiro, dispara um lembrete na hora marcada.</summary>
    public bool RemindAtStart { get; private set; }
    /// <summary>Minutos de antecedência para o lembrete (5/10/15/30), ou null.</summary>
    public int? ReminderMinutesBefore { get; private set; }
    /// <summary>Quando o lembrete de antecedência foi enviado (idempotência).</summary>
    public DateTime? ReminderBeforeSentAt { get; private set; }
    /// <summary>Quando o lembrete "na hora" foi enviado (idempotência).</summary>
    public DateTime? ReminderAtStartSentAt { get; private set; }

    /// <summary>
    /// Pontos de recompensa por concluir a missão, definidos pela prioridade.
    /// É uma propriedade calculada (get-only) — não há setter porque a regra é do domínio.
    /// </summary>
    public int RewardPoints => Priority switch
    {
        TaskPriority.Low => 10,
        TaskPriority.Medium => 20,
        TaskPriority.High => 30,
        _ => 0
    };

    // Construtor privado: só o EF Core usa (materialização a partir do banco).
    private TaskItem() : base() { }

    public TaskItem(
        string title,
        TaskCategory category,
        TaskPriority priority,
        DateOnly scheduledDate,
        TimeOnly scheduledTime,
        Guid assignedToId,
        Guid createdById,
        string? description = null,
        int? estimatedDurationMinutes = null,
        bool remindAtStart = false,
        int? reminderMinutesBefore = null)
        : base()
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("O título da missão é obrigatório.");
        if (assignedToId == Guid.Empty)
            throw new DomainException("A missão precisa estar atribuída a uma criança.");
        if (createdById == Guid.Empty)
            throw new DomainException("A missão precisa ter um responsável.");
        if (scheduledDate < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new DomainException("A data da missão não pode estar no passado.");
        if (reminderMinutesBefore is int mins && mins is not (5 or 10 or 15 or 30))
            throw new DomainException("Os minutos de antecedência devem ser 5, 10, 15 ou 30.");

        Title = title.Trim();
        Category = category;
        Priority = priority;
        ScheduledDate = scheduledDate;
        ScheduledTime = scheduledTime;
        AssignedToId = assignedToId;
        CreatedById = createdById;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        EstimatedDurationMinutes = estimatedDurationMinutes;
        RemindAtStart = remindAtStart;
        ReminderMinutesBefore = reminderMinutesBefore;
        Status = TaskStatus.Pending; // toda missão nova começa pendente
    }

    /// Inicia a missão. Só faz sentido partindo de Pending.
    public void Start()
    {
        if (Status != TaskStatus.Pending)
            throw new DomainException($"Não é possível iniciar uma missão com status '{Status}'.");

        Status = TaskStatus.InProgress;
        Touch();
    }

    /// <summary>
    /// Conclui a missão. A camada de aplicação deve ler RewardPoints
    /// e creditá-los à criança (Child.AddPoints) — o domínio orquestra o
    /// estado, a aplicação orquestra a transação entre agregados.
    /// </summary>
    public void Complete()
    {
        if (Status == TaskStatus.Completed)
            throw new DomainException("A missão já foi concluída.");
        if (Status == TaskStatus.Skipped)
            throw new DomainException("Não é possível concluir uma missão abandonada.");

        Status = TaskStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        Touch();
    }

    /// Abandona/pula a missão. Não concede pontos.
    public void Skip()
    {
        if (Status == TaskStatus.Completed)
            throw new DomainException("Não é possível abandonar uma missão já concluída.");

        Status = TaskStatus.Skipped;
        Touch();
    }

    /// <summary>Vincula esta instância a um grupo de recorrência (missões geradas juntas).</summary>
    public void SetRecurrenceGroup(Guid groupId)
    {
        if (groupId != Guid.Empty)
            RecurrenceGroupId = groupId;
    }

    // ====================== Lembretes: registro de envio (idempotência) ======================

    /// <summary>Registra o envio do lembrete de antecedência (chamado pelo ReminderService).</summary>
    public void MarkReminderBeforeSent()
    {
        ReminderBeforeSentAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>Registra o envio do lembrete "na hora" (chamado pelo ReminderService).</summary>
    public void MarkReminderAtStartSent()
    {
        ReminderAtStartSentAt = DateTime.UtcNow;
        Touch();
    }

    // ====================== Fluxo de aprovação com comprovação ======================

    /// <summary>
    /// A criança envia a foto de comprovação. A missão passa a aguardar a aprovação
    /// do responsável (PendingReview). Pode ser reenviada (limpa o comentário anterior).
    /// </summary>
    public void SubmitForReview(string imageUrl)
    {
        if (Status is TaskStatus.Completed or TaskStatus.Skipped)
            throw new DomainException($"Não é possível enviar comprovação de uma missão '{Status}'.");
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new DomainException("A comprovação precisa de uma imagem.");

        Status = TaskStatus.PendingReview;
        SubmissionImageUrl = imageUrl.Trim();
        SubmittedAt = DateTime.UtcNow;
        ReviewerComment = null; // limpa feedback de uma rejeição anterior ao reenviar
        Touch();
    }

    /// <summary>
    /// O responsável aprova a comprovação. Só faz sentido partindo de PendingReview.
    /// Os pontos são creditados pela camada de aplicação (Child.AddPoints).
    /// </summary>
    public void Approve()
    {
        if (Status == TaskStatus.Completed)
            throw new DomainException("A missão já foi concluída.");
        if (Status != TaskStatus.PendingReview)
            throw new DomainException("A missão não está aguardando aprovação.");

        Status = TaskStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>
    /// O responsável rejeita a comprovação com um comentário. A missão volta para
    /// InProgress (a criança refaz) e o comentário fica visível para ela.
    /// </summary>
    public void Reject(string comment)
    {
        if (Status != TaskStatus.PendingReview)
            throw new DomainException("A missão não está aguardando aprovação.");
        if (string.IsNullOrWhiteSpace(comment))
            throw new DomainException("Informe o que precisa ser revisto.");

        Status = TaskStatus.InProgress;
        ReviewerComment = comment.Trim();
        Touch();
    }
}
