using ControleFinanceiro.Domain.Contas;

namespace ControleFinanceiro.Application.Contas;

/// <summary>Coordena operações de contas e geração idempotente de compromissos.</summary>
public sealed class ServicoDeContas(
    IContasRepositorio repositorio,
    TimeProvider provedorDeTempo)
{
    /// <summary>Lista contas aplicando filtros e renova ocorrências mensais das contas ativas.</summary>
    /// <param name="filtro">Filtros por tipo, categoria, vencimento e inativação.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Contas correspondentes aos filtros, com seus compromissos persistidos.</returns>
    public async Task<IReadOnlyList<ContaDto>> ListarAsync(
        FiltroContas filtro,
        CancellationToken cancellationToken)
    {
        var contas = await repositorio.ListarAsync(cancellationToken);
        var hoje = DateOnly.FromDateTime(provedorDeTempo.GetLocalNow().DateTime);
        var alterou = RenovarOcorrencias(contas, hoje);
        if (alterou)
        {
            await repositorio.SalvarAsync(cancellationToken);
        }

        return contas
            .Where(conta => filtro.IncluirInativas || conta.Ativa)
            .Where(conta => filtro.Tipo is null || conta.Tipo == filtro.Tipo)
            .Where(conta => filtro.CategoriaId is null || conta.CategoriaContaId == filtro.CategoriaId)
            .Where(conta => filtro.SituacaoVencimento is null ||
                conta.Compromissos.Any(compromisso =>
                    !compromisso.Cancelado &&
                    compromisso.ClassificarVencimento(hoje) == filtro.SituacaoVencimento))
            .Select(conta => ParaDto(conta, hoje))
            .ToArray();
    }

    /// <summary>Consulta uma conta e renova suas ocorrências quando ela está ativa.</summary>
    /// <param name="id">Identificador da conta.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>A conta encontrada ou <see langword="null"/> quando não existe.</returns>
    public async Task<ContaDto?> ObterAsync(int id, CancellationToken cancellationToken)
    {
        var conta = await repositorio.ObterAsync(id, cancellationToken);
        if (conta is null)
        {
            return null;
        }

        var hoje = DateOnly.FromDateTime(provedorDeTempo.GetLocalNow().DateTime);
        if (RenovarOcorrencias([conta], hoje))
        {
            await repositorio.SalvarAsync(cancellationToken);
        }

        return ParaDto(conta, hoje);
    }

    /// <summary>Cria uma conta e os compromissos iniciais previstos para seu tipo.</summary>
    /// <param name="comando">Dados validados da nova conta.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>A conta persistida com seus compromissos.</returns>
    /// <exception cref="ExcecaoRegraDominio">Quando a categoria está indisponível ou os dados violam regras do domínio.</exception>
    public async Task<ContaDto> CriarAsync(CriarConta comando, CancellationToken cancellationToken)
    {
        var categoria = await repositorio.ObterCategoriaAtivaAsync(comando.CategoriaId, cancellationToken)
            ?? throw new ExcecaoRegraDominio("A categoria selecionada não existe ou está inativa.");
        var hoje = DateOnly.FromDateTime(provedorDeTempo.GetLocalNow().DateTime);
        var conta = new Conta(
            comando.Nome,
            comando.ValorPrevisto,
            comando.Descricao,
            categoria.Id,
            comando.Tipo,
            comando.VencimentoInicial,
            comando.TotalParcelas,
            comando.ParcelaAtual,
            hoje);

        repositorio.Adicionar(conta);
        await repositorio.SalvarAsync(cancellationToken);
        return ParaDto(conta, hoje);
    }

    /// <summary>Atualiza dados da conta sem reescrever compromissos vencidos ou com histórico.</summary>
    /// <param name="id">Identificador da conta.</param>
    /// <param name="comando">Novos dados editáveis.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>A conta atualizada ou <see langword="null"/> quando não existe.</returns>
    /// <exception cref="ExcecaoRegraDominio">Quando a categoria ou os dados atualizados são inválidos.</exception>
    public async Task<ContaDto?> AtualizarAsync(
        int id,
        AtualizarConta comando,
        CancellationToken cancellationToken)
    {
        var conta = await repositorio.ObterAsync(id, cancellationToken);
        if (conta is null)
        {
            return null;
        }

        var categoria = await repositorio.ObterCategoriaAtivaAsync(comando.CategoriaId, cancellationToken)
            ?? throw new ExcecaoRegraDominio("A categoria selecionada não existe ou está inativa.");
        var hoje = DateOnly.FromDateTime(provedorDeTempo.GetLocalNow().DateTime);
        int? sequenciaReferenciaMensal =
            conta.Tipo == TipoConta.FixaMensal &&
            conta.DataVencimentoInicial != comando.VencimentoInicial
                ? conta.ObterNovaSequenciaReferenciaMensal(comando.VencimentoInicial!.Value, hoje)
                : null;
        conta.Atualizar(
            comando.Nome,
            comando.ValorPrevisto,
            comando.Descricao,
            categoria.Id,
            comando.VencimentoInicial,
            sequenciaReferenciaMensal);

        var gerados = Compromisso.GerarIniciais(conta, hoje)
            .ToDictionary(compromisso => compromisso.Sequencia);
        foreach (var compromisso in conta.Compromissos)
        {
            var vencimentoFuturoOuIndefinido =
                compromisso.Vencimento is null || compromisso.Vencimento >= hoje;
            if (compromisso.SituacaoPagamento == SituacaoPagamento.Pendente &&
                !compromisso.PossuiHistoricoFinanceiro &&
                !compromisso.Cancelado &&
                vencimentoFuturoOuIndefinido)
            {
                if (conta.Tipo == TipoConta.FixaMensal)
                {
                    compromisso.AtualizarPrevisto(
                        conta.ValorPrevisto,
                        conta.CategoriaContaId,
                        Compromisso.CalcularVencimentoMensal(conta, compromisso.Sequencia),
                        provedorDeTempo.GetUtcNow(),
                        "Dados da conta atualizados");
                }
                else if (gerados.TryGetValue(compromisso.Sequencia, out var esperado))
                {
                    compromisso.AtualizarPrevisto(
                        esperado.ValorPrevisto,
                        esperado.CategoriaContaId,
                        esperado.Vencimento,
                        provedorDeTempo.GetUtcNow(),
                        "Dados da conta atualizados");
                }
                else
                {
                    compromisso.Cancelar(provedorDeTempo.GetUtcNow(), "Ocorrência removida da programação");
                }
            }
        }

        foreach (var novo in gerados.Values)
        {
            if (conta.Compromissos.All(existing => existing.Sequencia != novo.Sequencia))
            {
                conta.Compromissos.Add(novo);
            }
        }

        await repositorio.SalvarAsync(cancellationToken);
        return ParaDto(conta, hoje);
    }

    /// <summary>
    /// Inativa uma conta preservando registros e cancelando apenas compromissos
    /// futuros que ainda podem ser cancelados pela regra do domínio.
    /// </summary>
    /// <param name="id">Identificador da conta.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns><see langword="true"/> quando a conta foi encontrada; caso contrário, <see langword="false"/>.</returns>
    public async Task<bool> InativarAsync(int id, CancellationToken cancellationToken)
    {
        var conta = await repositorio.ObterAsync(id, cancellationToken);
        if (conta is null)
        {
            return false;
        }

        var hoje = DateOnly.FromDateTime(provedorDeTempo.GetLocalNow().DateTime);
        conta.Inativar();
        foreach (var compromisso in conta.Compromissos)
        {
            if (compromisso.Vencimento is null || compromisso.Vencimento >= hoje)
            {
                compromisso.Cancelar(provedorDeTempo.GetUtcNow(), "Conta inativada");
            }
        }

        await repositorio.SalvarAsync(cancellationToken);
        return true;
    }

    private bool RenovarOcorrencias(IEnumerable<Conta> contas, DateOnly hoje)
    {
        var alterou = false;
        foreach (var conta in contas.Where(conta => conta.Ativa && conta.Tipo == TipoConta.FixaMensal))
        {
            var existentes = conta.Compromissos.Select(compromisso => compromisso.Sequencia).ToHashSet();
            foreach (var ocorrencia in Compromisso.GerarOcorrenciasMensais(conta, hoje))
            {
                if (existentes.Add(ocorrencia.Sequencia))
                {
                    conta.Compromissos.Add(ocorrencia);
                    alterou = true;
                }
            }
        }

        return alterou;
    }

    private static ContaDto ParaDto(Conta conta, DateOnly hoje) =>
        new(
            conta.Id,
            conta.Nome,
            conta.ValorPrevisto,
            conta.Descricao,
            conta.CategoriaContaId,
            conta.Categoria?.Nome ?? string.Empty,
            conta.Tipo,
            conta.DataVencimentoInicial,
            conta.TotalParcelas,
            conta.ParcelaAtual,
            conta.Ativa,
            conta.Compromissos
                .OrderBy(compromisso => compromisso.Sequencia)
                .Select(compromisso => new CompromissoDto(
                    compromisso.Id,
                    compromisso.Sequencia,
                    compromisso.Vencimento,
                    compromisso.ValorPrevisto,
                    compromisso.SituacaoPagamento,
                    compromisso.ClassificarVencimento(hoje),
                    compromisso.Cancelado,
                    compromisso.CanceladoEm,
                    compromisso.MotivoCancelamento,
                    compromisso.Historico
                        .OrderBy(historico => historico.AlteradoEm)
                        .Select(historico => new HistoricoCompromissoDto(
                            historico.ValorAnterior,
                            historico.CategoriaAnteriorId,
                            historico.VencimentoAnterior,
                            historico.ValorNovo,
                            historico.CategoriaNovaId,
                            historico.VencimentoNovo,
                            historico.AlteradoEm,
                            historico.Motivo))
                        .ToArray()))
                .ToArray());
}
