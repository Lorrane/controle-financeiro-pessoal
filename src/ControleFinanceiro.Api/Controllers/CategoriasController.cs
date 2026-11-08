using System.ComponentModel.DataAnnotations;
using ControleFinanceiro.Application.Contas;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiro.Api.Controllers;

/// <summary>Expõe operações REST para o cadastro de categorias.</summary>
[ApiController]
[Route("api/categorias")]
public sealed class CategoriasController(ServicoDeCategorias servicoDeCategorias) : ControllerBase
{
    /// <summary>Lista categorias, incluindo inativas quando solicitado.</summary>
    /// <param name="incluirInativas">Indica se categorias inativas devem ser incluídas.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Lista de categorias correspondente ao filtro.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoriaDto>>> Listar(
        [FromQuery] bool incluirInativas,
        CancellationToken cancellationToken) =>
        Ok(await servicoDeCategorias.ListarAsync(incluirInativas, cancellationToken));

    /// <summary>Obtém uma categoria pelo identificador.</summary>
    /// <param name="id">Identificador da categoria.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>A categoria encontrada ou uma resposta 404.</returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoriaDto>> Obter(int id, CancellationToken cancellationToken)
    {
        var categoria = await servicoDeCategorias.ObterAsync(id, cancellationToken);
        return categoria is null ? NotFound() : Ok(categoria);
    }

    /// <summary>Cria uma categoria e retorna o recurso criado.</summary>
    /// <param name="requisicao">Nome e cor opcional da categoria.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>O recurso criado com sua localização.</returns>
    [HttpPost]
    public async Task<ActionResult<CategoriaDto>> Criar(
        [FromBody] CategoriaRequest requisicao,
        CancellationToken cancellationToken)
    {
        var categoria = await servicoDeCategorias.CriarAsync(
            new CriarCategoria(requisicao.Nome, requisicao.Cor),
            cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id = categoria.Id }, categoria);
    }

    /// <summary>Atualiza o nome e a cor de uma categoria existente.</summary>
    /// <param name="id">Identificador da categoria.</param>
    /// <param name="requisicao">Novo nome e cor opcional.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>A categoria atualizada ou uma resposta 404.</returns>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoriaDto>> Atualizar(
        int id,
        [FromBody] CategoriaRequest requisicao,
        CancellationToken cancellationToken)
    {
        var categoria = await servicoDeCategorias.AtualizarAsync(
            id,
            new AtualizarCategoria(requisicao.Nome, requisicao.Cor),
            cancellationToken);
        return categoria is null ? NotFound() : Ok(categoria);
    }

    /// <summary>Inativa uma categoria sem apagar dados associados.</summary>
    /// <param name="id">Identificador da categoria.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Resposta 204 se inativada ou 404 se não foi encontrada.</returns>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Inativar(int id, CancellationToken cancellationToken) =>
        await servicoDeCategorias.InativarAsync(id, cancellationToken) ? NoContent() : NotFound();

    /// <summary>Formato HTTP de entrada para criação e atualização de categoria.</summary>
    public sealed record CategoriaRequest(
        [Required, StringLength(80, MinimumLength = 1)] string Nome,
        [RegularExpression("^#[0-9A-Fa-f]{6}$")] string? Cor);
}
