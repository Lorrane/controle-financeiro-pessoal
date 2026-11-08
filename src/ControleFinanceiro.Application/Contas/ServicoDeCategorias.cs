using ControleFinanceiro.Domain.Contas;

namespace ControleFinanceiro.Application.Contas;

/// <summary>Executa cadastro, consulta, atualização e inativação de categorias.</summary>
public sealed class ServicoDeCategorias(ICategoriasRepositorio repositorio)
{
    /// <summary>Lista categorias, incluindo as inativas quando solicitado.</summary>
    /// <param name="incluirInativas">Indica se categorias inativas devem ser incluídas.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Categorias correspondentes ao filtro.</returns>
    public async Task<IReadOnlyList<CategoriaDto>> ListarAsync(
        bool incluirInativas,
        CancellationToken cancellationToken) =>
        (await repositorio.ListarAsync(incluirInativas, cancellationToken))
            .Select(ParaDto)
            .ToArray();

    /// <summary>Obtém uma categoria pelo identificador.</summary>
    /// <param name="id">Identificador da categoria.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>A categoria encontrada ou <see langword="null"/>.</returns>
    public async Task<CategoriaDto?> ObterAsync(int id, CancellationToken cancellationToken)
    {
        var categoria = await repositorio.ObterAsync(id, cancellationToken);
        return categoria is null ? null : ParaDto(categoria);
    }

    /// <summary>Cria uma categoria após verificar a unicidade do nome ativo.</summary>
    /// <param name="comando">Nome e cor opcionais da nova categoria.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>A categoria criada.</returns>
    /// <exception cref="ExcecaoRegraDominio">Quando o nome já está em uso ou os dados são inválidos.</exception>
    public async Task<CategoriaDto> CriarAsync(
        CriarCategoria comando,
        CancellationToken cancellationToken)
    {
        if (await repositorio.NomeEmUsoAsync(comando.Nome, null, cancellationToken))
        {
            throw new ExcecaoRegraDominio("Já existe uma categoria com esse nome.");
        }

        var categoria = new CategoriaConta(comando.Nome, comando.Cor);
        repositorio.Adicionar(categoria);
        await repositorio.SalvarAsync(cancellationToken);
        return ParaDto(categoria);
    }

    /// <summary>Atualiza uma categoria existente sem alterar sua identidade.</summary>
    /// <param name="id">Identificador da categoria.</param>
    /// <param name="comando">Novo nome e cor da categoria.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>A categoria atualizada ou <see langword="null"/> quando não existe.</returns>
    /// <exception cref="ExcecaoRegraDominio">Quando o nome já está em uso ou os dados são inválidos.</exception>
    public async Task<CategoriaDto?> AtualizarAsync(
        int id,
        AtualizarCategoria comando,
        CancellationToken cancellationToken)
    {
        var categoria = await repositorio.ObterAsync(id, cancellationToken);
        if (categoria is null)
        {
            return null;
        }

        if (await repositorio.NomeEmUsoAsync(comando.Nome, id, cancellationToken))
        {
            throw new ExcecaoRegraDominio("Já existe uma categoria com esse nome.");
        }

        categoria.Atualizar(comando.Nome, comando.Cor);
        await repositorio.SalvarAsync(cancellationToken);
        return ParaDto(categoria);
    }

    /// <summary>Inativa uma categoria sem apagar dados associados.</summary>
    /// <param name="id">Identificador da categoria.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns><see langword="true"/> quando encontrada; caso contrário, <see langword="false"/>.</returns>
    public async Task<bool> InativarAsync(int id, CancellationToken cancellationToken)
    {
        var categoria = await repositorio.ObterAsync(id, cancellationToken);
        if (categoria is null)
        {
            return false;
        }

        categoria.Inativar();
        await repositorio.SalvarAsync(cancellationToken);
        return true;
    }

    private static CategoriaDto ParaDto(CategoriaConta categoria) =>
        new(categoria.Id, categoria.Nome, categoria.Cor, categoria.Ativa);
}
