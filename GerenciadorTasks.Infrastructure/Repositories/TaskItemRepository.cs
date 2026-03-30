using GerenciadorTasks.Core.Entities;
using GerenciadorTasks.Core.Enums;
using GerenciadorTasks.Core.Interfaces.Repositories;
using GerenciadorTasks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using TaskStatus = GerenciadorTasks.Core.Enums.TaskStatus;

namespace GerenciadorTasks.Infrastructure.Repositories;

public class TaskItemRepository : ITaskItemRepository
{
    private readonly AppDbContext _context;

    public TaskItemRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TaskItems
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .Include(t => t.Justification)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> GetByAssignedToIdAsync(Guid assignedToId, TaskStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.TaskItems
            .Include(t => t.CreatedBy)
            .Where(t => t.AssignedToId == assignedToId);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> GetByCreatedByIdAsync(Guid createdById, CancellationToken cancellationToken = default)
    {
        return await _context.TaskItems
            .Include(t => t.AssignedTo)
            .Where(t => t.CreatedById == createdById)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> GetPendingByChildIdAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        return await _context.TaskItems
            .Where(t => t.AssignedToId == childId && t.Status == TaskStatus.Pending)
            .OrderBy(t => t.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await _context.TaskItems.AddAsync(task, cancellationToken);
    }

    public void Update(TaskItem task)
    {
        _context.TaskItems.Update(task);
    }
}
