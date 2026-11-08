using ControleFinanceiro.Application.Contas;
using ControleFinanceiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ControleFinanceiro.Infrastructure;

/// <summary>Registra serviços e persistência da camada de infraestrutura.</summary>
public static class ConfiguracaoInfraestrutura
{
    /// <summary>Adiciona o contexto SQLite e seus repositórios ao contêiner de dependências.</summary>
    /// <param name="servicos">Contêiner de serviços da aplicação.</param>
    /// <param name="configuracao">Configuração que contém a cadeia de conexão Financeiro.</param>
    /// <returns>O mesmo contêiner para encadeamento de registros adicionais.</returns>
    /// <exception cref="InvalidOperationException">Quando a cadeia de conexão não está configurada.</exception>
    public static IServiceCollection RegistrarInfraestrutura(
        this IServiceCollection servicos,
        IConfiguration configuracao)
    {
        var cadeiaDeConexao = configuracao.GetConnectionString("Financeiro")
            ?? throw new InvalidOperationException(
                "A string de conexão 'Financeiro' não foi configurada.");

        servicos.AddDbContext<ControleFinanceiroDbContext>(opcoes =>
            opcoes.UseSqlite(cadeiaDeConexao));
        servicos.AddScoped<ContasRepositorio>();
        servicos.AddScoped<IContasRepositorio>(provedorServicos =>
            provedorServicos.GetRequiredService<ContasRepositorio>());
        servicos.AddScoped<ICategoriasRepositorio>(provedorServicos =>
            provedorServicos.GetRequiredService<ContasRepositorio>());

        return servicos;
    }
}
