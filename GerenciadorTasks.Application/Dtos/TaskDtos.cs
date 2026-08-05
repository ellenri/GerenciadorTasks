using GerenciadorTasks.Application.Mapping;
using GerenciadorTasks.Core.Entities;

namespace GerenciadorTasks.Application.Dtos;

/// <summary>
/// Payload que o frontend ENVIA ao criar uma missão.
/// Espelha o "TaskFormData" do Astro (types.ts).
/// Usa strings (category/priority/date) no formato que o front trabalha.
/// </summary>
public record CreateTaskRequest(
    string Title,
    string? Description,
    string Category,        // "school", "personal_care", ...
    string Priority,        // "low", "medium", "high"
    string ScheduledDate,   // "YYYY-MM-DD"
    string ScheduledTime,   // "HH:mm"
    Guid AssignedTo,        // id da criança
    string? EstimatedDuration); // minutos como texto ("15", "30", ...)

/// <summary>
/// Payload que a API DEVOLVE. Leitura: do domínio para o JSON.
/// As datas/horas são formatadas manualmente para casar com o front ("yyyy-MM-dd"/"HH:mm").
/// </summary>
public record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    string Category,
    string Priority,
    string Status,
    string ScheduledDate,
    string ScheduledTime,
    Guid AssignedTo,
    int? EstimatedDuration,
    Guid CreatedBy,
    int RewardPoints,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? CompletedAt)
{
    /// Factory: constrói o DTO a partir da entidade de domínio.
    /// Centraliza o mapeamento em um lugar só (Single Responsibility).
    public static TaskResponse From(TaskItem t) => new(
        Id: t.Id,
        Title: t.Title,
        Description: t.Description,
        Category: EnumMapper.ToSnakeCase(t.Category),
        Priority: EnumMapper.ToSnakeCase(t.Priority),
        Status: EnumMapper.ToSnakeCase(t.Status),
        ScheduledDate: t.ScheduledDate.ToString("yyyy-MM-dd"),
        ScheduledTime: t.ScheduledTime.ToString("HH:mm"),
        AssignedTo: t.AssignedToId,
        EstimatedDuration: t.EstimatedDurationMinutes,
        CreatedBy: t.CreatedById,
        RewardPoints: t.RewardPoints,
        CreatedAt: t.CreatedAt,
        UpdatedAt: t.UpdatedAt,
        CompletedAt: t.CompletedAt);
}
