using GerenciadorTasks.Application.Abstractions;
using GerenciadorTasks.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorTasks.Infrastructure.Persistence;

/// <summary>Implementação EF Core de INotificationRepository.</summary>
public class EfNotificationRepository : INotificationRepository
{
    private readonly AppDbContext _db;

    public EfNotificationRepository(AppDbContext db) => _db = db;

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Notifications.FindAsync(new object[] { id }, ct);

    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _db.Notifications.AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

    public async Task<int> CountUnreadAsync(Guid userId, CancellationToken ct = default)
        => await _db.Notifications.AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public Task AddAsync(Notification notification, CancellationToken ct = default)
    {
        _db.Notifications.Add(notification);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Notification notification, CancellationToken ct = default)
    {
        _db.Notifications.Update(notification);
        return Task.CompletedTask;
    }
}
