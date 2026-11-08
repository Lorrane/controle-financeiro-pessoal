using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace ControleFinanceiro.Domain.Tests.Infrastructure;

public sealed class TestesDePersistenciaFinanceira
{
    [Fact]
    public void UtilizaProvedorSqlite()
    {
        var opcoes = new DbContextOptionsBuilder<ControleFinanceiroDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var contexto = new ControleFinanceiroDbContext(opcoes);

        Assert.True(contexto.Database.IsSqlite());
    }

    [Fact]
    public async Task MigrationPersisteContasComDecimaisExatosEDatasSemHorario()
    {
        await using var conexao = new SqliteConnection("Data Source=:memory:");
        await conexao.OpenAsync();
        var opcoes = new DbContextOptionsBuilder<ControleFinanceiroDbContext>()
            .UseSqlite(conexao)
            .Options;

        await using var contexto = new ControleFinanceiroDbContext(opcoes);
        await contexto.Database.MigrateAsync();
        var categoria = new CategoriaConta("Moradia");
        contexto.Categorias.Add(categoria);
        await contexto.SaveChangesAsync();

        var valor = 123456789012345678.123456789m;
        var vencimento = new DateOnly(2026, 8, 31);
        var conta = new Conta(
            "Aluguel",
            valor,
            "Valor de teste de precisão",
            categoria.Id,
            TipoConta.Unica,
            vencimento,
            null,
            null,
            new DateOnly(2026, 6, 1));
        contexto.Contas.Add(conta);
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();

        var persistida = await contexto.Contas
            .Include(conta => conta.Compromissos)
            .SingleAsync();

        Assert.Equal(valor, persistida.ValorPrevisto);
        Assert.Equal(vencimento, Assert.Single(persistida.Compromissos).Vencimento);
        Assert.Equal(valor, persistida.Compromissos.Single().ValorPrevisto);
    }
}
