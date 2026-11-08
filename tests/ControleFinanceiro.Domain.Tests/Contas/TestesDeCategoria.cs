using ControleFinanceiro.Domain.Contas;

namespace ControleFinanceiro.Domain.Tests.Contas;

public sealed class TestesDeCategoria
{
    [Fact]
    public void CorEhOpcionalENormalizadaParaMaiusculas()
    {
        var categoria = new CategoriaConta("Moradia", "#a1b2c3");

        Assert.Equal("#A1B2C3", categoria.Cor);

        categoria.Atualizar("Casa", null);

        Assert.Equal("Casa", categoria.Nome);
        Assert.Null(categoria.Cor);
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#12345")]
    [InlineData("#GG0000")]
    public void RejeitaCoresForaDoFormatoHexadecimalRgb(string cor)
    {
        Assert.Throws<ExcecaoRegraDominio>(() => new CategoriaConta("Moradia", cor));
    }
}
