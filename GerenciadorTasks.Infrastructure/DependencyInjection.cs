using GerenciadorTasks.Core.Interfaces;
using GerenciadorTasks.Core.Interfaces.Repositories;
using GerenciadorTasks.Infrastructure.Data;
using GerenciadorTasks.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GerenciadorTasks.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {


        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITaskItemRepository, TaskItemRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IRewardRepository, RewardRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
