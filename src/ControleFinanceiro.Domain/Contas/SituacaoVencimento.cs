namespace ControleFinanceiro.Domain.Contas;

/// <summary>Classifica a data de vencimento independentemente do pagamento.</summary>
public enum SituacaoVencimento
{
    SemVencimento = 1,
    EmDia = 2,
    Atrasado = 3
}
