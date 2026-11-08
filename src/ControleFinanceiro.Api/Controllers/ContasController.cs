using System.ComponentModel.DataAnnotations;
using ControleFinanceiro.Application.Contas;
using ControleFinanceiro.Domain.Contas;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiro.Api.Controllers;

/// <summary>Expõe operações REST para o cadastro de contas.</summary>
[ApiController]
[Route("api/contas")]
public sealed class ContasController(ServicoDeContas servicoDeContas) : ControllerBase
{
    /// <summary>Lista contas aplicando filtros de tipo, categoria, vencimento e inativação.</summary>
    /// <param name="tipo">Tipo de conta opcional.</param>
    /// <param name="categoriaId">Categoria opcional.</param>
    /// <param name="situacao">Classificação de vencimento opcional.</param>
    /// <param name="incluirInativas">Indica se contas inativas devem ser incluídas.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Lista de contas que corresponde aos filtros.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ContaDto>>> Listar(
        [FromQuery] TipoConta? tipo,
        [FromQuery] int? categoriaId,
        [FromQuery] SituacaoVencimento? situacao,
        [FromQuery] bool incluirInativas,
        CancellationToken cancellationToken)
    {
        var contas = await servicoDeContas.ListarAsync(
            new FiltroContas(tipo, categoriaId, situacao, incluirInativas),
            cancellationToken);
        return Ok(contas);
    }

    /// <summary>Obtém uma conta e seus compromissos pelo identificador.</summary>
    /// <param name="id">Identificador da conta.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>A conta encontrada ou uma resposta 404.</returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContaDto>> Obter(int id, CancellationToken cancellationToken)
    {
        var conta = await servicoDeContas.ObterAsync(id, cancellationToken);
        return conta is null ? NotFound() : Ok(conta);
    }

    /// <summary>Cria uma conta e retorna o recurso com os compromissos iniciais.</summary>
    /// <param name="requisicao">Dados validados da conta.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>O recurso criado com sua localização e compromissos iniciais.</returns>
    [HttpPost]
    public async Task<ActionResult<ContaDto>> Criar(
        [FromBody] CriarContaRequest requisicao,
        CancellationToken cancellationToken)
    {
        var conta = await servicoDeContas.CriarAsync(
            new CriarConta(
                requisicao.Nome,
                requisicao.ValorPrevisto,
                requisicao.Descricao,
                requisicao.CategoriaId,
                requisicao.Tipo,
                requisicao.VencimentoInicial,
                requisicao.TotalParcelas,
                requisicao.ParcelaAtual),
            cancellationToken);

        return CreatedAtAction(nameof(Obter), new { id = conta.Id }, conta);
    }

    /// <summary>Atualiza os dados editáveis de uma conta existente.</summary>
    /// <param name="id">Identificador da conta.</param>
    /// <param name="requisicao">Novos dados editáveis.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>A conta atualizada ou uma resposta 404.</returns>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ContaDto>> Atualizar(
        int id,
        [FromBody] AtualizarContaRequest requisicao,
        CancellationToken cancellationToken)
    {
        var conta = await servicoDeContas.AtualizarAsync(
            id,
            new AtualizarConta(
                requisicao.Nome,
                requisicao.ValorPrevisto,
                requisicao.Descricao,
                requisicao.CategoriaId,
                requisicao.VencimentoInicial),
            cancellationToken);
        return conta is null ? NotFound() : Ok(conta);
    }

    /// <summary>Inativa a conta sem remover seus compromissos ou históricos.</summary>
    /// <param name="id">Identificador da conta.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Resposta 204 se a conta foi inativada ou 404 se não foi encontrada.</returns>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Inativar(int id, CancellationToken cancellationToken) =>
        await servicoDeContas.InativarAsync(id, cancellationToken) ? NoContent() : NotFound();

    /// <summary>Formato HTTP de entrada para criação de conta.</summary>
    public sealed record CriarContaRequest(
        [Required, StringLength(120, MinimumLength = 1)] string Nome,
        decimal ValorPrevisto,
        [StringLength(500)] string? Descricao,
        [Range(1, int.MaxValue)] int CategoriaId,
        TipoConta Tipo,
        DateOnly? VencimentoInicial,
        [Range(1, int.MaxValue)] int? TotalParcelas,
        [Range(1, int.MaxValue)] int? ParcelaAtual);

    /// <summary>Formato HTTP de entrada para atualização de conta.</summary>
    public sealed record AtualizarContaRequest(
        [Required, StringLength(120, MinimumLength = 1)] string Nome,
        decimal ValorPrevisto,
        [StringLength(500)] string? Descricao,
        [Range(1, int.MaxValue)] int CategoriaId,
        DateOnly? VencimentoInicial);
}
