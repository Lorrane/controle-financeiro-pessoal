using ControleFinanceiro.Application.Contas;
using ControleFinanceiro.Domain.Contas;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Infrastructure.Persistence;

/// <summary>Implementa a persistência SQLite de contas e categorias.</summary>
public sealed class ContasRepositorio(ControleFinanceiroDbContext contextoBanco) :
    IContasRepositorio,
    ICategoriasRepositorio
{
    /// <summary>Carrega todas as contas com categoria, compromissos e histórico financeiro.</summary>
    public async Task<IReadOnlyList<Conta>> ListarAsync(CancellationToken cancellationToken) =>
        await contextoBanco.Contas
            .Include(conta => conta.Categoria)
            .Include(conta => conta.Compromissos)
                .ThenInclude(compromisso => compromisso.Historico)
            .ToListAsync(cancellationToken);

    /// <summary>Carrega uma conta com todos os dados associados pelo identificador.</summary>
    public Task<Conta?> ObterAsync(int id, CancellationToken cancellationToken) =>
        contextoBanco.Contas
            .Include(conta => conta.Categoria)
            .Include(conta => conta.Compromissos)
                .ThenInclude(compromisso => compromisso.Historico)
            .SingleOrDefaultAsync(conta => conta.Id == id, cancellationToken);

    /// <summary>Busca uma categoria ativa para associação a uma conta.</summary>
    public Task<CategoriaConta?> ObterCategoriaAtivaAsync(int id, CancellationToken cancellationToken) =>
        contextoBanco.Categorias
            .SingleOrDefaultAsync(categoria => categoria.Id == id && categoria.Ativa, cancellationToken);

    /// <summary>Adiciona uma conta ao contexto do Entity Framework.</summary>
    public void Adicionar(Conta conta) => contextoBanco.Contas.Add(conta);

    /// <summary>Lista categorias por nome, opcionalmente incluindo as inativas.</summary>
    public async Task<IReadOnlyList<CategoriaConta>> ListarAsync(
        bool incluirInativas,
        CancellationToken cancellationToken) =>
        await contextoBanco.Categorias
            .Where(categoria => incluirInativas || categoria.Ativa)
            .OrderBy(categoria => categoria.Nome)
            .ToListAsync(cancellationToken);

    Task<CategoriaConta?> ICategoriasRepositorio.ObterAsync(
        int id,
        CancellationToken cancellationToken) =>
        contextoBanco.Categorias.SingleOrDefaultAsync(categoria => categoria.Id == id, cancellationToken);

    /// <summary>Verifica se uma categoria ativa utiliza o nome informado.</summary>
    public Task<bool> NomeEmUsoAsync(
        string nome,
        int? ignorarId,
        CancellationToken cancellationToken)
    {
        var normalizado = nome.Trim();
        return contextoBanco.Categorias.AnyAsync(
            categoria => categoria.Ativa &&
                categoria.Id != ignorarId &&
                categoria.Nome == normalizado,
            cancellationToken);
    }

    /// <summary>Adiciona uma categoria ao contexto do Entity Framework.</summary>
    public void Adicionar(CategoriaConta categoria) => contextoBanco.Categorias.Add(categoria);

    /// <summary>Grava as alterações pendentes no banco SQLite.</summary>
    public Task SalvarAsync(CancellationToken cancellationToken) =>
        contextoBanco.SaveChangesAsync(cancellationToken);
}
