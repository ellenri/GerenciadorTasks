using GerenciadorTasks.Application.Abstractions;
using GerenciadorTasks.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorTasks.Infrastructure.Persistence;

/// <summary>
/// O "gerente do banco". Cada DbSet é uma tabela. O EF Core gera o SQL.
///
/// Implementa IUnitOfWork: o SaveChangesAsync (herdado do DbContext) confirma
/// todas as mudanças rastreadas numa única transação.
/// </summary>
public class AppDbContext : DbContext, IUnitOfWork
{
    public DbSet<Child> Children => Set<Child>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Reward> Rewards => Set<Reward>();

    // O EF injeta as opções (string de conexão, provider) via DI.
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Delega ao SaveChanges do DbContext (que satisfaz IUnitOfWork).
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => base.SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // CAMPO em vez de propriedade: nossas entidades têm 'private set', então
        // o EF precisa acessar via backing field para ler/escrever os valores.
        modelBuilder.UsePropertyAccessMode(PropertyAccessMode.Field);

        modelBuilder.Entity<Child>(b =>
        {
            b.ToTable("Children");
            b.HasKey(c => c.Id);
            b.Property(c => c.FullName).IsRequired().HasMaxLength(150);
            b.Property(c => c.AvatarPath).HasMaxLength(500);
        });

        modelBuilder.Entity<TaskItem>(b =>
        {
            b.ToTable("Tasks");
            b.HasKey(t => t.Id);
            b.Property(t => t.Title).IsRequired().HasMaxLength(200);
            b.Property(t => t.Description).HasMaxLength(1000);
            // Enums viram int (padrão). DateOnly/TimeOnly: o EF Core 8+ já
            // mapeia nativamente no SQLite (sem conversão manual).
        });

        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("Users");
            b.HasKey(u => u.Id);
            b.Property(u => u.FullName).IsRequired().HasMaxLength(150);
            b.Property(u => u.Email).IsRequired().HasMaxLength(254);
            b.Property(u => u.PasswordHash).HasMaxLength(500);
            b.HasIndex(u => u.Email).IsUnique(); // e-mail não pode repetir
        });

        modelBuilder.Entity<Reward>(b =>
        {
            b.ToTable("Rewards");
            b.HasKey(r => r.Id);
            b.Property(r => r.Title).IsRequired().HasMaxLength(150);
            b.Property(r => r.Description).HasMaxLength(1000);
            b.Property(r => r.RequiredPoints).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}
