namespace ControleFinanceiro.Domain.Contas;

/// <summary>Representa uma conta cadastrada e a programação de seus compromissos.</summary>
public sealed class Conta
{
    private Conta()
    {
    }

    /// <summary>Cria uma conta validada e gera seus compromissos iniciais.</summary>
    /// <param name="nome">Nome identificador da conta.</param>
    /// <param name="valorPrevisto">Valor positivo previsto por compromisso.</param>
    /// <param name="descricao">Detalhe opcional da conta.</param>
    /// <param name="categoriaId">Identificador da categoria associada.</param>
    /// <param name="tipo">Regra de geração dos compromissos.</param>
    /// <param name="vencimentoInicial">Primeiro vencimento, opcional apenas para conta única.</param>
    /// <param name="totalParcelas">Quantidade total, obrigatória para conta parcelada.</param>
    /// <param name="parcelaAtual">Parcela inicial a gerar para conta parcelada.</param>
    /// <param name="hoje">Data usada para limitar a geração mensal inicial.</param>
    /// <exception cref="ExcecaoRegraDominio">Quando os dados violam uma regra do domínio.</exception>
    public Conta(
        string nome,
        decimal valorPrevisto,
        string? descricao,
        int categoriaId,
        TipoConta tipo,
        DateOnly? vencimentoInicial,
        int? totalParcelas,
        int? parcelaAtual,
        DateOnly hoje)
    {
        Nome = ValidarNome(nome);
        ValorPrevisto = ValidarValor(valorPrevisto);
        Descricao = ValidarDescricao(descricao);
        CategoriaContaId = ValidarCategoriaId(categoriaId);
        Tipo = tipo;
        DataVencimentoInicial = vencimentoInicial;
        TotalParcelas = totalParcelas;
        ParcelaAtual = parcelaAtual;
        Ativa = true;
        SequenciaReferenciaMensal = 1;
        ValidarConfiguracao();
        Compromissos = Compromisso.GerarIniciais(this, hoje).ToList();
    }

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public decimal ValorPrevisto { get; private set; }
    public string? Descricao { get; private set; }
    public int CategoriaContaId { get; private set; }
    public CategoriaConta Categoria { get; private set; } = null!;
    public TipoConta Tipo { get; private set; }
    public DateOnly? DataVencimentoInicial { get; private set; }
    public int? TotalParcelas { get; private set; }
    public int? ParcelaAtual { get; private set; }
    public int SequenciaReferenciaMensal { get; private set; }
    public bool Ativa { get; private set; }
    public ICollection<Compromisso> Compromissos { get; private set; } = new List<Compromisso>();

    /// <summary>Atualiza os dados editáveis sem alterar o tipo nem a identidade da conta.</summary>
    /// <param name="nome">Novo nome da conta.</param>
    /// <param name="valorPrevisto">Novo valor previsto por compromisso.</param>
    /// <param name="descricao">Nova descrição opcional.</param>
    /// <param name="categoriaId">Identificador da nova categoria.</param>
    /// <param name="vencimentoInicial">Novo vencimento de referência.</param>
    /// <param name="sequenciaReferenciaMensal">Nova referência mensal, quando a data é reancorada.</param>
    /// <exception cref="ExcecaoRegraDominio">Quando os dados atualizados são inválidos.</exception>
    public void Atualizar(
        string nome,
        decimal valorPrevisto,
        string? descricao,
        int categoriaId,
        DateOnly? vencimentoInicial,
        int? sequenciaReferenciaMensal = null)
    {
        Nome = ValidarNome(nome);
        ValorPrevisto = ValidarValor(valorPrevisto);
        Descricao = ValidarDescricao(descricao);
        CategoriaContaId = ValidarCategoriaId(categoriaId);
        DataVencimentoInicial = vencimentoInicial;
        if (Tipo == TipoConta.FixaMensal && sequenciaReferenciaMensal is not null)
        {
            SequenciaReferenciaMensal = sequenciaReferenciaMensal.Value;
        }

        ValidarConfiguracao();
    }

    /// <summary>Calcula a sequência correspondente ao mês atual da recorrência.</summary>
    /// <param name="hoje">Data atual usada no cálculo.</param>
    /// <returns>A sequência da ocorrência referente ao mês atual ou posterior.</returns>
    /// <exception cref="ExcecaoRegraDominio">Quando a conta não é mensal ou não tem vencimento.</exception>
    public int ObterSequenciaReferenciaMensal(DateOnly hoje)
    {
        if (Tipo != TipoConta.FixaMensal || DataVencimentoInicial is null)
        {
            throw new ExcecaoRegraDominio("Somente contas fixas mensais possuem sequência de recorrência.");
        }

        var mesesDecorridos = Math.Max(
            0,
            ((hoje.Year - DataVencimentoInicial.Value.Year) * 12) +
            hoje.Month -
            DataVencimentoInicial.Value.Month);
        return SequenciaReferenciaMensal + mesesDecorridos;
    }

    /// <summary>Reancora a data mensal preservando a sequência já estabelecida.</summary>
    /// <param name="novoVencimento">Nova data-base da recorrência.</param>
    /// <param name="hoje">Data atual usada para preservar a sequência.</param>
    /// <returns>A sequência de referência compatível com a nova data-base.</returns>
    /// <exception cref="ExcecaoRegraDominio">Quando a reancoragem precede a primeira sequência.</exception>
    public int ObterNovaSequenciaReferenciaMensal(DateOnly novoVencimento, DateOnly hoje)
    {
        if (Tipo != TipoConta.FixaMensal || DataVencimentoInicial is null)
        {
            throw new ExcecaoRegraDominio("Somente contas fixas mensais possuem sequência de recorrência.");
        }

        var mesesDecorridos = Math.Max(
            0,
            ((hoje.Year - DataVencimentoInicial.Value.Year) * 12) +
            hoje.Month -
            DataVencimentoInicial.Value.Month);
        var mesesAteHojeNaNovaReferencia = Math.Max(
            0,
            ((hoje.Year - novoVencimento.Year) * 12) +
            hoje.Month -
            novoVencimento.Month);
        var sequenciaAtual = SequenciaReferenciaMensal + mesesDecorridos;
        var novaReferencia = sequenciaAtual - mesesAteHojeNaNovaReferencia;
        if (novaReferencia < 1)
        {
            throw new ExcecaoRegraDominio("A data mensal não pode anteceder a primeira ocorrência da conta.");
        }

        return novaReferencia;
    }

    /// <summary>Inativa a conta sem remover seus compromissos.</summary>
    public void Inativar() => Ativa = false;

    private void ValidarConfiguracao()
    {
        if (!Enum.IsDefined(Tipo))
        {
            throw new ExcecaoRegraDominio("O tipo de conta informado é inválido.");
        }

        switch (Tipo)
        {
            case TipoConta.Unica when TotalParcelas is not null || ParcelaAtual is not null:
                throw new ExcecaoRegraDominio("Conta única não possui quantidade ou número de parcelas.");
            case TipoConta.Parcelada when DataVencimentoInicial is null:
                throw new ExcecaoRegraDominio("A data de vencimento é obrigatória para contas parceladas.");
            case TipoConta.Parcelada when TotalParcelas is null or < 1:
                throw new ExcecaoRegraDominio("Informe uma quantidade total de parcelas maior que zero.");
            case TipoConta.Parcelada when ParcelaAtual is null ||
                ParcelaAtual.Value < 1 ||
                ParcelaAtual.Value > TotalParcelas!.Value:
                throw new ExcecaoRegraDominio("A parcela atual deve estar entre 1 e o total de parcelas.");
            case TipoConta.FixaMensal when DataVencimentoInicial is null:
                throw new ExcecaoRegraDominio("A data de vencimento é obrigatória para contas fixas mensais.");
            case TipoConta.FixaMensal when TotalParcelas is not null || ParcelaAtual is not null:
                throw new ExcecaoRegraDominio("Conta fixa mensal não possui quantidade final de parcelas.");
            case TipoConta.FixaMensal when SequenciaReferenciaMensal < 1:
                throw new ExcecaoRegraDominio("A sequência da recorrência deve ser maior que zero.");
        }
    }

    private static string ValidarNome(string nome)
    {
        var valor = nome?.Trim();
        if (string.IsNullOrWhiteSpace(valor) || valor.Length > 120)
        {
            throw new ExcecaoRegraDominio("Informe um nome de conta com até 120 caracteres.");
        }

        return valor;
    }

    private static decimal ValidarValor(decimal valor)
    {
        if (valor <= 0)
        {
            throw new ExcecaoRegraDominio("O valor previsto deve ser maior que zero.");
        }

        return valor;
    }

    private static string? ValidarDescricao(string? descricao)
    {
        var valor = descricao?.Trim();
        if (valor?.Length > 500)
        {
            throw new ExcecaoRegraDominio("A descrição deve ter até 500 caracteres.");
        }

        return string.IsNullOrEmpty(valor) ? null : valor;
    }

    private static int ValidarCategoriaId(int categoriaId)
    {
        if (categoriaId <= 0)
        {
            throw new ExcecaoRegraDominio("Selecione uma categoria válida.");
        }

        return categoriaId;
    }
}
