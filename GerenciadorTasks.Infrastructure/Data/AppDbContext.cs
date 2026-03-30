using GerenciadorTasks.Core.Entities;
using GerenciadorTasks.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorTasks.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Reward> Rewards => Set<Reward>();
    public DbSet<Justification> Justifications => Set<Justification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.FullName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).IsRequired();

            entity.HasIndex(e => e.Email).IsUnique();

            entity.HasOne(e => e.Parent)
                .WithMany(p => p.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Navigation(e => e.CreatedTasks).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(e => e.AssignedTasks).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Status).IsRequired();

            entity.HasOne(e => e.CreatedBy)
                .WithMany(u => u.CreatedTasks)
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AssignedTo)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(e => e.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Justification)
                .WithOne(j => j.TaskItem)
                .HasForeignKey<Justification>(j => j.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.Type).IsRequired();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Reward>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.RequiredPoints).IsRequired();

            entity.HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.RedeemedBy)
                .WithMany()
                .HasForeignKey(e => e.RedeemedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Justification>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Reason).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}
