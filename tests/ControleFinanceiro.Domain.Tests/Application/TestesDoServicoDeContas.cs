using ControleFinanceiro.Application.Contas;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Domain.Tests.Application;

public sealed class TestesDoServicoDeContas
{
    [Fact]
    public async Task AlteracaoDeDataMensalPreservaEAuditaCompromissosSemDuplicarSequencias()
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

        var servico = new ServicoDeContas(
            new ContasRepositorio(contexto),
            new ProvedorDeTempoFixo(new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero)));
        var criada = await servico.CriarAsync(
            new CriarConta(
                "Aluguel",
                1000m,
                null,
                categoria.Id,
                TipoConta.FixaMensal,
                new DateOnly(2026, 1, 31),
                null,
                null),
            CancellationToken.None);
        var estadoAnterior = criada.Compromissos.ToDictionary(compromisso => compromisso.Sequencia);

        var atualizada = await servico.AtualizarAsync(
            criada.Id,
            new AtualizarConta(
                "Aluguel",
                1100m,
                null,
                categoria.Id,
                new DateOnly(2026, 12, 15)),
            CancellationToken.None);

        Assert.NotNull(atualizada);
        Assert.Equal(estadoAnterior.Keys.Order(), atualizada.Compromissos.Select(item => item.Sequencia).Order());
        Assert.Equal(12, atualizada.Compromissos.Count);
        foreach (var compromisso in atualizada.Compromissos)
        {
            Assert.False(compromisso.Cancelado);
            Assert.Equal(1100m, compromisso.ValorPrevisto);
            Assert.Single(compromisso.Historico);
            Assert.Equal(
                estadoAnterior[compromisso.Sequencia].Vencimento,
                compromisso.Historico[0].VencimentoAnterior);
        }

        var listaAtualizada = await servico.ListarAsync(
            new FiltroContas(null, null, null, false),
            CancellationToken.None);
        Assert.Equal(12, Assert.Single(listaAtualizada).Compromissos.Count);
    }


    [Fact]
    public async Task DevePreservarCompromissosVencidosAoAlterarValorDaConta()
    {
        // ORGANIZAR: criar um banco SQLite temporário em memória.
        await using var conexao = new SqliteConnection("Data Source=:memory:");
        await conexao.OpenAsync();

        var opcoes = new DbContextOptionsBuilder<ControleFinanceiroDbContext>()
            .UseSqlite(conexao)
            .Options;

        await using var contexto = new ControleFinanceiroDbContext(opcoes);
        await contexto.Database.MigrateAsync();

        // Criar uma categoria necessária para cadastrar a conta.
        var categoria = new CategoriaConta("Moradia");
        contexto.Categorias.Add(categoria);
        await contexto.SaveChangesAsync();

        // Permite avançar a data simulada sem depender do relógio real.
        var relogio = new RelogioDeTeste(
            new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

        var servico = new ServicoDeContas(
            new ContasRepositorio(contexto),
            relogio);

        // Cadastrar a conta em janeiro, com valor original de R$ 1.000.
        var contaCriada = await servico.CriarAsync(
            new CriarConta(
                "Aluguel",
                1000m,
                null,
                categoria.Id,
                TipoConta.FixaMensal,
                new DateOnly(2026, 1, 10),
                null,
                null),
            CancellationToken.None);

        // EXECUTAR: avançar o relógio para novembro.
        relogio.DefinirData(
            new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero));

        // Alterar somente o valor previsto da conta.
        var contaAtualizada = await servico.AtualizarAsync(
            contaCriada.Id,
            new AtualizarConta(
                "Aluguel",
                1100m,
                null,
                categoria.Id,
                new DateOnly(2026, 1, 10)),
            CancellationToken.None);

        Assert.NotNull(contaAtualizada);

        // VERIFICAR: as parcelas vencidas devem conservar R$ 1.000.
        var parcelasVencidas = contaAtualizada.Compromissos
            .Where(compromisso =>
                compromisso.Vencimento.HasValue &&
                compromisso.Vencimento.Value <
                    new DateOnly(2026, 11, 8))
            .ToArray();

        Assert.Equal(10, parcelasVencidas.Length);

        Assert.All(parcelasVencidas, compromisso =>
            Assert.Equal(1000m, compromisso.ValorPrevisto));

        // VERIFICAR: os compromissos futuros podem receber R$ 1.100.
        var parcelasFuturas = contaAtualizada.Compromissos
            .Where(compromisso =>
                compromisso.Vencimento.HasValue &&
                compromisso.Vencimento.Value >
                    new DateOnly(2026, 11, 8))
            .ToArray();

        Assert.NotEmpty(parcelasFuturas);

        Assert.All(parcelasFuturas, compromisso =>
            Assert.Equal(1100m, compromisso.ValorPrevisto));
    }

    // Relógio controlável para simular a passagem do tempo nos testes.
    private sealed class RelogioDeTeste(DateTimeOffset dataInicial) : TimeProvider
    {
        private DateTimeOffset dataAtual = dataInicial;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => dataAtual;

        public void DefinirData(DateTimeOffset novaData)
        {
            dataAtual = novaData;
        }
    }


    [Fact]
    public async Task DeveRenovarRecorrenciasSemDuplicarCompromissosNoBanco()
    {
        // ORGANIZAR: criar um banco SQLite temporário em memória.
        await using var conexao = new SqliteConnection("Data Source=:memory:");
        await conexao.OpenAsync();

        var opcoes = new DbContextOptionsBuilder<ControleFinanceiroDbContext>()
            .UseSqlite(conexao)
            .Options;

        await using var contexto = new ControleFinanceiroDbContext(opcoes);
        await contexto.Database.MigrateAsync();

        // Criar a categoria necessária para a conta.
        var categoria = new CategoriaConta("Moradia");
        contexto.Categorias.Add(categoria);
        await contexto.SaveChangesAsync();

        // Começar o teste em janeiro de 2026.
        var relogio = new RelogioRenovacao(
            new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

        var servico = new ServicoDeContas(
            new ContasRepositorio(contexto),
            relogio);

        // Cadastrar uma conta mensal com vencimento no dia 31.
        var conta = await servico.CriarAsync(
            new CriarConta(
                "Aluguel",
                1000m,
                null,
                categoria.Id,
                TipoConta.FixaMensal,
                new DateOnly(2026, 1, 31),
                null,
                null),
            CancellationToken.None);

        // VERIFICAR: inicialmente devem existir 12 compromissos.
        Assert.Equal(12, conta.Compromissos.Count);

        // EXECUTAR: consultar repetidamente no mesmo mês.
        for (int tentativa = 0; tentativa < 5; tentativa++)
        {
            await servico.ListarAsync(
                new FiltroContas(null, null, null, false),
                CancellationToken.None);
        }

        // Avançar o relógio para fevereiro.
        relogio.DefinirData(
            new DateTimeOffset(2026, 2, 1, 12, 0, 0, TimeSpan.Zero));

        // Consultar novamente várias vezes.
        for (int tentativa = 0; tentativa < 5; tentativa++)
        {
            await servico.ListarAsync(
                new FiltroContas(null, null, null, false),
                CancellationToken.None);
        }

        // Abrir outro contexto para verificar os dados persistidos,
        // evitando depender apenas das entidades rastreadas pelo primeiro.
        await using var contextoVerificacao =
            new ControleFinanceiroDbContext(opcoes);

        var contaPersistida = await contextoVerificacao.Contas
            .AsNoTracking()
            .Include(item => item.Compromissos)
            .SingleAsync(item => item.Id == conta.Id);

        var compromissos = contaPersistida.Compromissos.ToArray();

        // VERIFICAR: fevereiro acrescenta somente a sequência 13.
        Assert.Equal(13, compromissos.Length);

        // VERIFICAR: cada sequência deve existir uma única vez.
        Assert.Equal(
            compromissos.Length,
            compromissos.Select(item => item.Sequencia).Distinct().Count());

        // VERIFICAR: as 12 ocorrências anteriores foram preservadas.
        Assert.Equal(
            Enumerable.Range(1, 13),
            compromissos.Select(item => item.Sequencia).Order());

        // VERIFICAR: a nova ocorrência vence em janeiro de 2027.
        var novaOcorrencia = compromissos.Single(
            item => item.Sequencia == 13);

        Assert.Equal(
            new DateOnly(2027, 1, 31),
            novaOcorrencia.Vencimento);
    }

    [Fact]
    public async Task InativarContaPreservaHistoricoEAtrasadosECancelaFuturosSemHistorico()
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

        var relogio = new RelogioRenovacao(
            new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var servico = new ServicoDeContas(new ContasRepositorio(contexto), relogio);
        var criada = await servico.CriarAsync(
            new CriarConta(
                "Aluguel",
                1000m,
                null,
                categoria.Id,
                TipoConta.FixaMensal,
                new DateOnly(2026, 1, 10),
                null,
                null),
            CancellationToken.None);
        var compromissoComHistorico = contexto.Entry(
            await contexto.Contas
                .Include(conta => conta.Compromissos)
                .SingleAsync(conta => conta.Id == criada.Id))
            .Entity.Compromissos.Single(compromisso => compromisso.Sequencia == 2);
        compromissoComHistorico.RegistrarHistoricoFinanceiro();
        await contexto.SaveChangesAsync();

        relogio.DefinirData(new DateTimeOffset(2026, 2, 1, 12, 0, 0, TimeSpan.Zero));
        var inativada = await servico.InativarAsync(criada.Id, CancellationToken.None);

        Assert.True(inativada);
        Assert.False(compromissoComHistorico.Cancelado);
        Assert.True(await servico.InativarAsync(criada.Id, CancellationToken.None));

        var compromissosPersistidos = await contexto.Compromissos
            .Where(compromisso => compromisso.ContaId == criada.Id)
            .OrderBy(compromisso => compromisso.Sequencia)
            .ToArrayAsync();
        Assert.Equal(12, compromissosPersistidos.Length);
        Assert.False(compromissosPersistidos[0].Cancelado);
        Assert.True(compromissosPersistidos[1].PossuiHistoricoFinanceiro);
        Assert.False(compromissosPersistidos[1].Cancelado);
        Assert.All(compromissosPersistidos.Skip(2), compromisso => Assert.True(compromisso.Cancelado));
    }

    // Relógio controlável para simular a passagem dos meses.
    private sealed class RelogioRenovacao(DateTimeOffset dataInicial)
        : TimeProvider
    {
        private DateTimeOffset dataAtual = dataInicial;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => dataAtual;

        public void DefinirData(DateTimeOffset novaData)
        {
            dataAtual = novaData;
        }
    }

    private sealed class ProvedorDeTempoFixo(DateTimeOffset agora) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => agora;
    }
}
