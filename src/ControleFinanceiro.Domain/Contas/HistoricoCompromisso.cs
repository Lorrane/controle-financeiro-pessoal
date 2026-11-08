namespace ControleFinanceiro.Domain.Contas;

/// <summary>Preserva os valores antes e depois de uma alteração em compromisso.</summary>
public sealed class HistoricoCompromisso
{
    private HistoricoCompromisso()
    {
    }

    internal HistoricoCompromisso(
        int compromissoId,
        decimal valorAnterior,
        int categoriaAnteriorId,
        DateOnly? vencimentoAnterior,
        decimal valorNovo,
        int categoriaNovaId,
        DateOnly? vencimentoNovo,
        DateTimeOffset alteradoEm,
        string motivo)
    {
        CompromissoId = compromissoId;
        ValorAnterior = valorAnterior;
        CategoriaAnteriorId = categoriaAnteriorId;
        VencimentoAnterior = vencimentoAnterior;
        ValorNovo = valorNovo;
        CategoriaNovaId = categoriaNovaId;
        VencimentoNovo = vencimentoNovo;
        AlteradoEm = alteradoEm;
        Motivo = motivo;
    }

    public int Id { get; private set; }
    public int CompromissoId { get; private set; }
    public decimal ValorAnterior { get; private set; }
    public int CategoriaAnteriorId { get; private set; }
    public DateOnly? VencimentoAnterior { get; private set; }
    public decimal ValorNovo { get; private set; }
    public int CategoriaNovaId { get; private set; }
    public DateOnly? VencimentoNovo { get; private set; }
    public DateTimeOffset AlteradoEm { get; private set; }
    public string Motivo { get; private set; } = string.Empty;
}
