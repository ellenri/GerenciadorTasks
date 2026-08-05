using GerenciadorTasks.Application.Abstractions;
using GerenciadorTasks.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorTasks.Infrastructure.Persistence;

/// <summary>Implementação EF Core de IUserRepository.</summary>
public class EfUserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public EfUserRepository(AppDbContext db) => _db = db;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Users.FindAsync(new object[] { id }, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default)
        => await _db.Users.AsNoTracking().ToListAsync(ct);

    public Task AddAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        return Task.CompletedTask;
    }
}
