using GerenciadorTasks.Core.Entities;
using GerenciadorTasks.Core.Enums;
using TaskStatus = GerenciadorTasks.Core.Enums.TaskStatus;

namespace GerenciadorTasks.Core.Interfaces.Repositories;

public interface ITaskItemRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> GetByAssignedToIdAsync(Guid assignedToId, TaskStatus? status = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> GetByCreatedByIdAsync(Guid createdById, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> GetPendingByChildIdAsync(Guid childId, CancellationToken cancellationToken = default);
    Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);
    void Update(TaskItem task);
}
