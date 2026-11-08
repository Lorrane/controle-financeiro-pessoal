using System.Text.RegularExpressions;

namespace ControleFinanceiro.Domain.Contas;

/// <summary>Categoria nomeada e colorida opcionalmente para organizar contas.</summary>
public sealed class CategoriaConta
{
    private CategoriaConta()
    {
    }

    /// <summary>Cria uma categoria ativa após validar nome e cor.</summary>
    /// <param name="nome">Nome obrigatório da categoria.</param>
    /// <param name="cor">Cor opcional no formato hexadecimal #RRGGBB.</param>
    /// <exception cref="ExcecaoRegraDominio">Quando o nome ou a cor são inválidos.</exception>
    public CategoriaConta(string nome, string? cor = null)
    {
        Nome = ValidarNome(nome);
        Cor = ValidarCor(cor);
    }

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string? Cor { get; private set; }
    public bool Ativa { get; private set; } = true;
    public ICollection<Conta> Contas { get; private set; } = new List<Conta>();

    /// <summary>Atualiza o nome e a cor sem alterar a identidade da categoria.</summary>
    /// <param name="nome">Novo nome obrigatório.</param>
    /// <param name="cor">Nova cor opcional no formato hexadecimal #RRGGBB.</param>
    /// <exception cref="ExcecaoRegraDominio">Quando o nome ou a cor são inválidos.</exception>
    public void Atualizar(string nome, string? cor)
    {
        Nome = ValidarNome(nome);
        Cor = ValidarCor(cor);
    }

    /// <summary>Inativa a categoria sem remover contas ou compromissos associados.</summary>
    public void Inativar() => Ativa = false;

    private static string ValidarNome(string nome)
    {
        var valor = nome?.Trim();
        if (string.IsNullOrWhiteSpace(valor) || valor.Length > 80)
        {
            throw new ExcecaoRegraDominio("Informe um nome de categoria com até 80 caracteres.");
        }

        return valor;
    }

    private static string? ValidarCor(string? cor)
    {
        var valor = cor?.Trim();
        if (string.IsNullOrEmpty(valor))
        {
            return null;
        }

        if (!Regex.IsMatch(valor, "^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant))
        {
            throw new ExcecaoRegraDominio("A cor deve estar no formato hexadecimal #RRGGBB.");
        }

        return valor.ToUpperInvariant();
    }
}
