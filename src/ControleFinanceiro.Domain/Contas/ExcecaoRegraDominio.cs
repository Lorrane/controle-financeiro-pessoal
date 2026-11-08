namespace ControleFinanceiro.Domain.Contas;

/// <summary>Indica que uma operação viola uma regra do domínio financeiro.</summary>
public sealed class ExcecaoRegraDominio(string mensagem) : Exception(mensagem)
{
}
