using GerenciadorTasks.Application.Abstractions;
using GerenciadorTasks.Application.Services;
using GerenciadorTasks.Infrastructure.Persistence;
using GerenciadorTasksApi.ExceptionHandling;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins("http://localhost:4321")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// ====================== Persistência (SQLite + EF Core) ======================
// AddDbContext registra o AppDbContext (Scoped) e define o provider SQLite.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// IUnitOfWork aponta para a MESMA instância do DbContext compartilhada no request:
// assim o serviço chama SaveChanges uma vez e confirma tudo (transação atômica).
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

// ============ Inversão de Dependência (o coração desta rodada) ============
// Trocamos a IMPLEMENTAÇÃO de memória -> EF Core. As INTERFACES não mudaram,
// então TaskService/ChildService seguem IDÊNTICOS. Só a "instalação" mudou.
// Para voltar a memória, troque estas duas linhas e use InMemoryUnitOfWork:
//   builder.Services.AddSingleton<ITaskRepository, InMemoryTaskRepository>();
//   builder.Services.AddSingleton<IChildRepository, InMemoryChildRepository>();
//   builder.Services.AddSingleton<IUnitOfWork, InMemoryUnitOfWork>();
builder.Services.AddScoped<ITaskRepository, EfTaskRepository>();
builder.Services.AddScoped<IChildRepository, EfChildRepository>();

builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<ChildService>();

var app = builder.Build();

// Cria o banco e popula os dados iniciais (idempotente).
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;

    var db = sp.GetRequiredService<AppDbContext>();
    // Aplica migrations pendentes (cria o banco se não existir). Ao contrário de
    // EnsureCreated, suporta evoluir o schema com novas migrations no futuro.
    await db.Database.MigrateAsync();

    var children = sp.GetRequiredService<IChildRepository>();
    await SeedData.InitializeAsync(children);
    // O repo só rastreia; confirma de fato as crianças adicionadas pelo seed.
    await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseCors();
app.MapControllers();

app.Run();
