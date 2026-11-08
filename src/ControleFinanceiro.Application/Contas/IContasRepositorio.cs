using ControleFinanceiro.Domain.Contas;

namespace ControleFinanceiro.Application.Contas;

/// <summary>Define operações de persistência necessárias aos casos de uso de contas.</summary>
public interface IContasRepositorio
{
    /// <summary>Carrega todas as contas com categorias, compromissos e histórico.</summary>
    Task<IReadOnlyList<Conta>> ListarAsync(CancellationToken cancellationToken);
    /// <summary>Carrega uma conta e seus dados associados pelo identificador.</summary>
    Task<Conta?> ObterAsync(int id, CancellationToken cancellationToken);
    /// <summary>Obtém uma categoria que ainda pode ser associada a uma conta.</summary>
    Task<CategoriaConta?> ObterCategoriaAtivaAsync(int id, CancellationToken cancellationToken);
    /// <summary>Adiciona uma conta ao contexto de persistência.</summary>
    void Adicionar(Conta conta);
    /// <summary>Persiste as alterações pendentes.</summary>
    Task SalvarAsync(CancellationToken cancellationToken);
}
