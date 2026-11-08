namespace ControleFinanceiro.Domain.Contas;

/// <summary>Define as regras disponíveis para gerar compromissos de uma conta.</summary>
public enum TipoConta
{
    Unica = 1,
    Parcelada = 2,
    FixaMensal = 3
}
