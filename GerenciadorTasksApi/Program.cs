using GerenciadorTasks.Application.Abstractions;
using GerenciadorTasks.Application.Services;
using GerenciadorTasks.Infrastructure.Persistence;
using GerenciadorTasks.Infrastructure.Security;
using GerenciadorTasksApi.ExceptionHandling;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins("http://localhost:4321")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()); // necessário para enviar o cookie de auth cross-origin
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// ====================== Autenticação (cookie HttpOnly) ======================
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        // Lax funciona em dev (mesmo host: localhost). Em produção cross-domain,
        // troque por SameSite=None + Cookie.SecurePolicy=Always (exige HTTPS).
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        // API não deve redirecionar (302) em 401/403 — devolve o status puro.
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

// ====================== Persistência (SQLite + EF Core) ======================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// IUnitOfWork aponta para a MESMA instância do DbContext compartilhada no request:
// assim o serviço chama SaveChanges uma vez e confirma tudo (transação atômica).
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

// Repositórios EF Core (interface em Application, implementação em Infrastructure).
builder.Services.AddScoped<ITaskRepository, EfTaskRepository>();
builder.Services.AddScoped<IChildRepository, EfChildRepository>();
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<IRewardRepository, EfRewardRepository>();
builder.Services.AddScoped<INotificationRepository, EfNotificationRepository>();

// Hash de senhas.
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();

// Casos de uso (serviços de aplicação).
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<ChildService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<RewardService>();
builder.Services.AddScoped<NotificationService>();

var app = builder.Build();

// Cria/atualiza o banco (migrations) e popula os dados iniciais (idempotente).
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;

    var db = sp.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    await SeedData.InitializeAsync(
        sp.GetRequiredService<IUserRepository>(),
        sp.GetRequiredService<IChildRepository>(),
        sp.GetRequiredService<IRewardRepository>(),
        sp.GetRequiredService<IPasswordHasher>(),
        sp.GetRequiredService<IUnitOfWork>());
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();   // identifica o usuário a partir do cookie
app.UseAuthorization();    // aplica [Authorize] / roles
app.MapControllers();

app.Run();
