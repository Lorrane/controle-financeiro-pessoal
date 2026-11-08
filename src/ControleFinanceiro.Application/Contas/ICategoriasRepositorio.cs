using ControleFinanceiro.Domain.Contas;

namespace ControleFinanceiro.Application.Contas;

/// <summary>Define operações de persistência necessárias aos casos de uso de categorias.</summary>
public interface ICategoriasRepositorio
{
    /// <summary>Lista categorias ativas ou todas, conforme o filtro.</summary>
    Task<IReadOnlyList<CategoriaConta>> ListarAsync(bool incluirInativas, CancellationToken cancellationToken);
    /// <summary>Obtém uma categoria pelo identificador.</summary>
    Task<CategoriaConta?> ObterAsync(int id, CancellationToken cancellationToken);
    /// <summary>Verifica duplicidade entre categorias ativas, opcionalmente ignorando uma delas.</summary>
    Task<bool> NomeEmUsoAsync(string nome, int? ignorarId, CancellationToken cancellationToken);
    /// <summary>Adiciona uma categoria ao contexto de persistência.</summary>
    void Adicionar(CategoriaConta categoria);
    /// <summary>Persiste as alterações pendentes.</summary>
    Task SalvarAsync(CancellationToken cancellationToken);
}
