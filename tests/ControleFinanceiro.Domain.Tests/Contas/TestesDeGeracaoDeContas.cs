using ControleFinanceiro.Domain.Contas;

namespace ControleFinanceiro.Domain.Tests.Contas;

public sealed class TestesDeGeracaoDeContas
{
    [Fact]
    public void ContaUnicaSemVencimentoGeraUmCompromissoPendenteSemData()
    {
        var conta = CriarConta(TipoConta.Unica, null, null, null, new DateOnly(2026, 6, 10));

        var compromisso = Assert.Single(conta.Compromissos);
        Assert.Null(compromisso.Vencimento);
        Assert.Equal(1, compromisso.Sequencia);
        Assert.Equal(SituacaoPagamento.Pendente, compromisso.SituacaoPagamento);
        Assert.Equal(SituacaoVencimento.SemVencimento, compromisso.ClassificarVencimento(new DateOnly(2026, 6, 10)));
    }

    [Fact]
    public void ContaParceladaGeraSomenteParcelaAtualEParcelasRestantes()
    {
        var conta = CriarConta(
            TipoConta.Parcelada,
            new DateOnly(2026, 7, 31),
            totalParcelas: 12,
            parcelaAtual: 7,
            hoje: new DateOnly(2026, 7, 1));

        Assert.Equal(6, conta.Compromissos.Count);
        Assert.Equal(new[] { 7, 8, 9, 10, 11, 12 }, conta.Compromissos.Select(item => item.Sequencia));
        Assert.Equal(
            new[]
            {
                new DateOnly(2026, 7, 31),
                new DateOnly(2026, 8, 31),
                new DateOnly(2026, 9, 30),
                new DateOnly(2026, 10, 31),
                new DateOnly(2026, 11, 30),
                new DateOnly(2026, 12, 31)
            },
            conta.Compromissos.Select(item => item.Vencimento!.Value));
    }

    [Fact]
    public void ContaMensalGeraHorizonteDeDozeMesesSemDeslocarDataBase()
    {
        var conta = CriarConta(
            TipoConta.FixaMensal,
            new DateOnly(2026, 1, 31),
            totalParcelas: null,
            parcelaAtual: null,
            hoje: new DateOnly(2026, 1, 1));

        Assert.Equal(12, conta.Compromissos.Count);
        Assert.Equal(1, conta.Compromissos.Min(item => item.Sequencia));
        Assert.Equal(12, conta.Compromissos.Max(item => item.Sequencia));
        Assert.Equal(new DateOnly(2026, 2, 28), conta.Compromissos.Single(item => item.Sequencia == 2).Vencimento);
        Assert.Equal(new DateOnly(2026, 3, 31), conta.Compromissos.Single(item => item.Sequencia == 3).Vencimento);
        Assert.Equal(new DateOnly(2026, 12, 31), conta.Compromissos.Single(item => item.Sequencia == 12).Vencimento);
    }

    [Fact]
    public void RenovacaoMensalMantemReferenciaOriginalESemRepetirSequencias()
    {
        var conta = CriarConta(
            TipoConta.FixaMensal,
            new DateOnly(2026, 1, 31),
            totalParcelas: null,
            parcelaAtual: null,
            hoje: new DateOnly(2026, 1, 1));
        var existentes = conta.Compromissos.Select(item => item.Sequencia).ToHashSet();
        var renovacoes = Compromisso.GerarOcorrenciasMensais(conta, new DateOnly(2026, 2, 1))
            .Where(item => existentes.Add(item.Sequencia))
            .ToArray();

        Assert.Single(renovacoes);
        Assert.Equal(13, renovacoes[0].Sequencia);
        Assert.Equal(new DateOnly(2027, 1, 31), renovacoes[0].Vencimento);
    }

    [Fact]
    public void AlteracaoDeVencimentoMensalPreservaSequenciaEReferenciaDoDia()
    {
        var hoje = new DateOnly(2026, 11, 8);
        var conta = CriarConta(
            TipoConta.FixaMensal,
            new DateOnly(2026, 1, 31),
            totalParcelas: null,
            parcelaAtual: null,
            hoje);
        var sequenciaReferencia = conta.ObterNovaSequenciaReferenciaMensal(
            new DateOnly(2026, 12, 31),
            hoje);

        conta.Atualizar(
            conta.Nome,
            conta.ValorPrevisto,
            conta.Descricao,
            conta.CategoriaContaId,
            new DateOnly(2026, 12, 31),
            sequenciaReferencia);
        var compromissos = Compromisso.GerarIniciais(conta, hoje).ToArray();

        Assert.Equal(11, conta.ObterSequenciaReferenciaMensal(hoje));
        Assert.Equal(11, sequenciaReferencia);
        Assert.Equal(11, compromissos[0].Sequencia);
        Assert.Equal(new DateOnly(2026, 12, 31), compromissos[0].Vencimento);
        Assert.Equal(21, compromissos[^1].Sequencia);
        Assert.Equal(new DateOnly(2027, 10, 31), compromissos[^1].Vencimento);
        Assert.Equal(
            new DateOnly(2026, 11, 30),
            Compromisso.CalcularVencimentoMensal(conta, 10));
    }

    [Fact]
    public void CompromissoPendentePodeSerClassificadoComoAtrasado()
    {
        var conta = CriarConta(
            TipoConta.Unica,
            new DateOnly(2026, 6, 1),
            null,
            null,
            new DateOnly(2026, 6, 2));
        var compromisso = Assert.Single(conta.Compromissos);

        Assert.Equal(SituacaoPagamento.Pendente, compromisso.SituacaoPagamento);
        Assert.Equal(SituacaoVencimento.Atrasado, compromisso.ClassificarVencimento(new DateOnly(2026, 6, 2)));
    }

    [Fact]
    public void AtualizacaoDeCompromissoRegistraSnapshotEImpedeAlteracaoComHistorico()
    {
        var conta = CriarConta(
            TipoConta.Unica,
            new DateOnly(2026, 7, 1),
            null,
            null,
            new DateOnly(2026, 6, 1));
        var compromisso = Assert.Single(conta.Compromissos);
        compromisso.AtualizarPrevisto(
            140m,
            2,
            new DateOnly(2026, 7, 15),
            DateTimeOffset.UtcNow,
            "Teste de alteração");

        var historico = Assert.Single(compromisso.Historico);
        Assert.Equal(125.50m, historico.ValorAnterior);
        Assert.Equal(140m, historico.ValorNovo);
        Assert.Equal(new DateOnly(2026, 7, 1), historico.VencimentoAnterior);
        Assert.Equal(new DateOnly(2026, 7, 15), historico.VencimentoNovo);

        compromisso.RegistrarHistoricoFinanceiro();
        Assert.Throws<ExcecaoRegraDominio>(() =>
            compromisso.AtualizarPrevisto(
                150m,
                2,
                new DateOnly(2026, 7, 20),
                DateTimeOffset.UtcNow,
                "Alteração bloqueada"));
    }

    [Theory]
    [InlineData(TipoConta.Parcelada, null, 12, 1)]
    [InlineData(TipoConta.FixaMensal, null, null, null)]
    public void ContasRecorrentesExigemVencimentoInicial(
        TipoConta tipo,
        DateOnly? vencimento,
        int? totalParcelas,
        int? parcelaAtual)
    {
        Assert.Throws<ExcecaoRegraDominio>(() =>
            CriarConta(tipo, vencimento, totalParcelas, parcelaAtual, new DateOnly(2026, 1, 1)));
    }

    [Theory]
    [InlineData(12, 0)]
    [InlineData(12, 13)]
    public void NumeroDaParcelaDeveEstarDentroDoTotal(int totalParcelas, int parcelaAtual)
    {
        Assert.Throws<ExcecaoRegraDominio>(() =>
            CriarConta(
                TipoConta.Parcelada,
                new DateOnly(2026, 1, 1),
                totalParcelas,
                parcelaAtual,
                new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public void ContaUnicaNaoAceitaNumeracaoDeParcelas()
    {
        Assert.Throws<ExcecaoRegraDominio>(() =>
            CriarConta(
                TipoConta.Unica,
                null,
                totalParcelas: 1,
                parcelaAtual: 1,
                hoje: new DateOnly(2026, 1, 1)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValorDaContaDeveSerPositivo(decimal valor)
    {
        Assert.Throws<ExcecaoRegraDominio>(() =>
            new Conta(
                "Conta inválida",
                valor,
                null,
                1,
                TipoConta.Unica,
                null,
                null,
                null,
                new DateOnly(2026, 1, 1)));
    }


    [Fact]
    public void DeveRespeitarFevereiroEmAnoBissexto()
    {
        // ORGANIZAR: criar uma conta mensal com vencimento no dia 31.
        var conta = CriarConta(
            TipoConta.FixaMensal,
            new DateOnly(2028, 1, 31),
            null,
            null,
            new DateOnly(2028, 1, 1));

        // EXECUTAR: localizar as ocorrências de fevereiro e março.
        var fevereiro = conta.Compromissos.Single(
            compromisso => compromisso.Sequencia == 2);

        var marco = conta.Compromissos.Single(
            compromisso => compromisso.Sequencia == 3);

        // VERIFICAR: fevereiro tem 29 dias em 2028.
        Assert.Equal(
            new DateOnly(2028, 2, 29),
            fevereiro.Vencimento);

        // VERIFICAR: março deve voltar ao dia 31.
        Assert.Equal(
            new DateOnly(2028, 3, 31),
            marco.Vencimento);
    }

    [Fact]
    public void DeveGerarSomenteUltimaParcelaQuandoAtualForIgualAoTotal()
    {
        // ORGANIZAR: conta que está na última parcela (12/12).
        var conta = CriarConta(
            TipoConta.Parcelada,
            new DateOnly(2026, 11, 15),
            12,
            12,
            new DateOnly(2026, 11, 1));

        // VERIFICAR: somente uma parcela deve existir.
        var compromisso = Assert.Single(conta.Compromissos);

        Assert.Equal(12, compromisso.Sequencia);
        Assert.Equal(
            new DateOnly(2026, 11, 15),
            compromisso.Vencimento);
        Assert.Equal(
            SituacaoPagamento.Pendente,
            compromisso.SituacaoPagamento);
    }

    [Fact]
    public void CompromissoNaoDeveSerAlteradoAposRegistrarHistoricoFinanceiro()
    {
        // ORGANIZAR: criar uma conta única com vencimento futuro.
        var conta = CriarConta(
            TipoConta.Unica,
            new DateOnly(2026, 12, 10),
            null,
            null,
            new DateOnly(2026, 11, 8));

        var compromisso = Assert.Single(conta.Compromissos);

        // Simular a existência de histórico financeiro.
        compromisso.RegistrarHistoricoFinanceiro();

        // EXECUTAR: tentar modificar o compromisso protegido.
        Assert.Throws<ExcecaoRegraDominio>(() =>
            compromisso.AtualizarPrevisto(
                200m,
                1,
                new DateOnly(2026, 12, 20),
                DateTimeOffset.UtcNow,
                "Tentativa de alteração"));

        // VERIFICAR: os valores originais permanecem intactos.
        Assert.Equal(125.50m, compromisso.ValorPrevisto);
        Assert.Equal(
            new DateOnly(2026, 12, 10),
            compromisso.Vencimento);
    }

    private static Conta CriarConta(
        TipoConta tipo,
        DateOnly? vencimento,
        int? totalParcelas,
        int? parcelaAtual,
        DateOnly hoje) =>
        new("Conta de teste", 125.50m, null, 1, tipo, vencimento, totalParcelas, parcelaAtual, hoje);
}
