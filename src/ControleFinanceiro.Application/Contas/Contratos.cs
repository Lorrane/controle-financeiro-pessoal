using ControleFinanceiro.Domain.Contas;

namespace ControleFinanceiro.Application.Contas;

/// <summary>Dados necessários para criar uma conta e seus compromissos iniciais.</summary>
public sealed record CriarConta(
    string Nome,
    decimal ValorPrevisto,
    string? Descricao,
    int CategoriaId,
    TipoConta Tipo,
    DateOnly? VencimentoInicial,
    int? TotalParcelas,
    int? ParcelaAtual);

/// <summary>Dados editáveis de uma conta existente.</summary>
public sealed record AtualizarConta(
    string Nome,
    decimal ValorPrevisto,
    string? Descricao,
    int CategoriaId,
    DateOnly? VencimentoInicial);

/// <summary>Representação de leitura da conta e de seus compromissos.</summary>
public sealed record ContaDto(
    int Id,
    string Nome,
    decimal ValorPrevisto,
    string? Descricao,
    int CategoriaId,
    string CategoriaNome,
    TipoConta Tipo,
    DateOnly? VencimentoInicial,
    int? TotalParcelas,
    int? ParcelaAtual,
    bool Ativa,
    IReadOnlyList<CompromissoDto> Compromissos);

/// <summary>Representação de leitura de uma parcela ou ocorrência mensal.</summary>
public sealed record CompromissoDto(
    int Id,
    int Sequencia,
    DateOnly? Vencimento,
    decimal ValorPrevisto,
    SituacaoPagamento SituacaoPagamento,
    SituacaoVencimento SituacaoVencimento,
    bool Cancelado,
    DateTimeOffset? CanceladoEm,
    string? MotivoCancelamento,
    IReadOnlyList<HistoricoCompromissoDto> Historico);

/// <summary>Snapshot anterior e novo de uma alteração auditada.</summary>
public sealed record HistoricoCompromissoDto(
    decimal ValorAnterior,
    int CategoriaAnteriorId,
    DateOnly? VencimentoAnterior,
    decimal ValorNovo,
    int CategoriaNovaId,
    DateOnly? VencimentoNovo,
    DateTimeOffset AlteradoEm,
    string Motivo);

/// <summary>Representação de leitura de uma categoria.</summary>
public sealed record CategoriaDto(int Id, string Nome, string? Cor, bool Ativa);

/// <summary>Dados necessários para cadastrar uma categoria.</summary>
public sealed record CriarCategoria(string Nome, string? Cor);

/// <summary>Dados editáveis de uma categoria existente.</summary>
public sealed record AtualizarCategoria(string Nome, string? Cor);

/// <summary>Filtros usados para consultar contas.</summary>
public sealed record FiltroContas(
    TipoConta? Tipo,
    int? CategoriaId,
    SituacaoVencimento? SituacaoVencimento,
    bool IncluirInativas);
