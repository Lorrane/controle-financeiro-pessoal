namespace ControleFinanceiro.Domain.Contas;

/// <summary>Representa uma parcela ou ocorrência individual vinculada à conta original.</summary>
public sealed class Compromisso
{
    private Compromisso()
    {
    }

    private Compromisso(Conta conta, int sequencia, DateOnly? vencimento)
    {
        Conta = conta;
        ContaId = conta.Id;
        Sequencia = sequencia;
        Vencimento = vencimento;
        ValorPrevisto = conta.ValorPrevisto;
        CategoriaContaId = conta.CategoriaContaId;
        SituacaoPagamento = SituacaoPagamento.Pendente;
    }

    public int Id { get; private set; }
    public int ContaId { get; private set; }
    public Conta Conta { get; private set; } = null!;
    public int Sequencia { get; private set; }
    public DateOnly? Vencimento { get; private set; }
    public decimal ValorPrevisto { get; private set; }
    public int CategoriaContaId { get; private set; }
    public SituacaoPagamento SituacaoPagamento { get; private set; }
    public bool PossuiHistoricoFinanceiro { get; private set; }
    public bool Cancelado { get; private set; }
    public DateTimeOffset? CanceladoEm { get; private set; }
    public string? MotivoCancelamento { get; private set; }
    public ICollection<HistoricoCompromisso> Historico { get; private set; } =
        new List<HistoricoCompromisso>();

    /// <summary>Classifica o vencimento independentemente da situação de pagamento.</summary>
    /// <param name="hoje">Data local usada para comparar o vencimento.</param>
    /// <returns>Classificação sem vencimento, em dia ou atrasado.</returns>
    public SituacaoVencimento ClassificarVencimento(DateOnly hoje)
    {
        if (Vencimento is null)
        {
            return SituacaoVencimento.SemVencimento;
        }

        return Vencimento < hoje
            ? SituacaoVencimento.Atrasado
            : SituacaoVencimento.EmDia;
    }

    /// <summary>Marca que existe histórico financeiro que deve ser preservado.</summary>
    public void RegistrarHistoricoFinanceiro() => PossuiHistoricoFinanceiro = true;

    /// <summary>Gera os compromissos iniciais conforme o tipo e a configuração da conta.</summary>
    /// <param name="conta">Conta de origem dos compromissos.</param>
    /// <param name="hoje">Data atual para limitar a geração mensal.</param>
    /// <returns>Compromissos únicos, parcelas restantes ou ocorrências mensais dentro do horizonte.</returns>
    public static IEnumerable<Compromisso> GerarIniciais(Conta conta, DateOnly hoje)
    {
        switch (conta.Tipo)
        {
            case TipoConta.Unica:
                yield return new Compromisso(conta, 1, conta.DataVencimentoInicial);
                break;
            case TipoConta.Parcelada:
                var parcelaAtual = conta.ParcelaAtual!.Value;
                for (var sequencia = parcelaAtual; sequencia <= conta.TotalParcelas; sequencia++)
                {
                    yield return new Compromisso(
                        conta,
                        sequencia,
                        DataDaParcela(conta.DataVencimentoInicial!.Value, parcelaAtual, sequencia));
                }

                break;
            case TipoConta.FixaMensal:
                foreach (var compromisso in GerarOcorrenciasMensais(conta, hoje))
                {
                    yield return compromisso;
                }

                break;
        }
    }

    /// <summary>Gera ocorrências mensais até o limite móvel de 12 meses.</summary>
    /// <param name="conta">Conta fixa mensal a processar.</param>
    /// <param name="hoje">Data que ancora o horizonte de geração.</param>
    /// <returns>Ocorrências dentro do horizonte, sem criar parcelas ilimitadas.</returns>
    public static IEnumerable<Compromisso> GerarOcorrenciasMensais(Conta conta, DateOnly hoje)
    {
        if (conta.Tipo != TipoConta.FixaMensal || conta.DataVencimentoInicial is null)
        {
            yield break;
        }

        var dataInicial = conta.DataVencimentoInicial.Value;
        var deslocamentoInicial = Math.Max(0, MesesEntre(dataInicial, hoje));
        var limite = hoje.AddMonths(12);

        for (var deslocamento = deslocamentoInicial; ; deslocamento++)
        {
            var sequencia = conta.SequenciaReferenciaMensal + deslocamento;
            var vencimento = CalcularVencimentoMensal(conta, sequencia);
            if (vencimento >= limite)
            {
                yield break;
            }

            yield return new Compromisso(conta, sequencia, vencimento);
        }
    }

    /// <summary>Calcula o vencimento mensal sem deslocar o dia de referência original.</summary>
    /// <param name="conta">Conta fixa mensal de referência.</param>
    /// <param name="sequencia">Número sequencial da ocorrência.</param>
    /// <returns>Vencimento da ocorrência, ajustado ao último dia do mês quando necessário.</returns>
    /// <exception cref="ExcecaoRegraDominio">Quando a conta não tem uma referência mensal válida.</exception>
    public static DateOnly CalcularVencimentoMensal(Conta conta, int sequencia)
    {
        if (conta.Tipo != TipoConta.FixaMensal || conta.DataVencimentoInicial is null)
        {
            throw new ExcecaoRegraDominio("Somente contas fixas mensais possuem vencimentos recorrentes.");
        }

        return DataDoMes(
            conta.DataVencimentoInicial.Value,
            sequencia - conta.SequenciaReferenciaMensal);
    }

    /// <summary>Atualiza dados previstos e registra os valores substituídos no histórico.</summary>
    /// <param name="valor">Novo valor previsto.</param>
    /// <param name="categoriaContaId">Nova categoria.</param>
    /// <param name="vencimento">Novo vencimento.</param>
    /// <param name="alteradoEm">Instante da alteração.</param>
    /// <param name="motivo">Motivo registrado para auditoria.</param>
    /// <exception cref="ExcecaoRegraDominio">Quando o compromisso pago, cancelado ou histórico não pode ser alterado.</exception>
    public void AtualizarPrevisto(
        decimal valor,
        int categoriaContaId,
        DateOnly? vencimento,
        DateTimeOffset alteradoEm,
        string motivo)
    {
        if (SituacaoPagamento != SituacaoPagamento.Pendente ||
            PossuiHistoricoFinanceiro ||
            Cancelado)
        {
            throw new ExcecaoRegraDominio("Este compromisso não pode ser alterado.");
        }

        if (ValorPrevisto == valor && CategoriaContaId == categoriaContaId && Vencimento == vencimento)
        {
            return;
        }

        Historico.Add(new HistoricoCompromisso(
            Id,
            ValorPrevisto,
            CategoriaContaId,
            Vencimento,
            valor,
            categoriaContaId,
            vencimento,
            alteradoEm,
            motivo));

        ValorPrevisto = valor;
        CategoriaContaId = categoriaContaId;
        Vencimento = vencimento;
    }

    /// <summary>
    /// Cancela compromisso pendente sem histórico; compromissos pagos, cancelados ou
    /// com histórico são preservados sem alteração.
    /// </summary>
    /// <param name="canceladoEm">Instante do cancelamento.</param>
    /// <param name="motivo">Motivo a manter para rastreabilidade.</param>
    public void Cancelar(DateTimeOffset canceladoEm, string motivo)
    {
        if (SituacaoPagamento != SituacaoPagamento.Pendente || PossuiHistoricoFinanceiro || Cancelado)
        {
            return;
        }

        Cancelado = true;
        CanceladoEm = canceladoEm;
        MotivoCancelamento = motivo;
    }

    private static DateOnly DataDaParcela(DateOnly vencimentoAtual, int parcelaAtual, int sequencia) =>
        DataDoMes(vencimentoAtual, sequencia - parcelaAtual);

    private static DateOnly DataDoMes(DateOnly dataBase, int mesesAdiante)
    {
        var primeiroDia = new DateOnly(dataBase.Year, dataBase.Month, 1).AddMonths(mesesAdiante);
        var dia = Math.Min(dataBase.Day, DateTime.DaysInMonth(primeiroDia.Year, primeiroDia.Month));
        return new DateOnly(primeiroDia.Year, primeiroDia.Month, dia);
    }

    private static int MesesEntre(DateOnly inicio, DateOnly fim) =>
        ((fim.Year - inicio.Year) * 12) + fim.Month - inicio.Month;
}
