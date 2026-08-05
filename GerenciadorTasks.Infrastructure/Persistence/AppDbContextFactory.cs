using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GerenciadorTasks.Infrastructure.Persistence;

/// <summary>
/// Fábrica usada SOMENTE em design-time pelo <c>dotnet ef</c>
/// (<c>migrations add</c>, <c>database update</c>, <c>dbcontext info</c>).
///
/// Permite que o EF Core crie um <see cref="AppDbContext"/> sem precisar
/// construir o host da API inteiro (o que rodaria seed, CORS, etc.).
/// A connection string aqui precisa bater com a usada em runtime
/// (appsettings.json do projeto GerenciadorTasksApi).
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=gerenciador.db")
            .Options;

        return new AppDbContext(options);
    }
}
