using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GerenciadorTasks.Infrastructure.Data;

/// <summary>
/// Fábrica usada SOMENTE em design-time (quando você roda
/// `dotnet ef migrations add` ou `dotnet ef database update`).
///
/// O EF Core procura uma classe que implemente IDesignTimeDbContextFactory
/// antes de tentar instanciar o host da aplicação. Isso evita que o EF
/// precise rodar o Program.cs inteiro da API só pra criar o DbContext.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Em design-time não dá pra confiar no appsettings.json estar acessível
        // da mesma forma que em runtime. Por isso usamos uma connection string
        // fixa apontando para o mesmo arquivo configurado em produção.
        var connectionString = "Data Source=gerenciador.db";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
