using ControleFinanceiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ControleFinanceiro.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Financeiro")
            ?? throw new InvalidOperationException(
                "A string de conexão 'Financeiro' não foi configurada.");

        services.AddDbContext<ControleFinanceiroDbContext>(options =>
            options.UseSqlite(connectionString));

        return services;
    }
}
