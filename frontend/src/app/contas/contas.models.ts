/** Tipos de programação disponíveis para uma conta. */
export type TipoConta = 'unica' | 'parcelada' | 'fixaMensal';
/** Estados financeiros de pagamento conhecidos pelo modelo. */
export type SituacaoPagamento = 'pendente' | 'pago';
/** Classificações de vencimento independentes do estado de pagamento. */
export type SituacaoVencimento = 'semVencimento' | 'emDia' | 'atrasado';

/** Categoria persistida, com cor opcional para a apresentação dos cartões. */
export interface Categoria {
  id: number;
  nome: string;
  cor: string | null;
  ativa: boolean;
}

/** Parcela ou ocorrência vinculada à conta de origem. */
export interface Compromisso {
  id: number;
  sequencia: number;
  vencimento: string | null;
  valorPrevisto: number;
  situacaoPagamento: SituacaoPagamento;
  situacaoVencimento: SituacaoVencimento;
  cancelado: boolean;
}

/** Conta com configuração, categoria e compromissos retornados pela API. */
export interface Conta {
  id: number;
  nome: string;
  valorPrevisto: number;
  descricao: string | null;
  categoriaId: number;
  categoriaNome: string;
  tipo: TipoConta;
  vencimentoInicial: string | null;
  totalParcelas: number | null;
  parcelaAtual: number | null;
  ativa: boolean;
  compromissos: Compromisso[];
}

/** Valores editáveis do formulário de criação e atualização de contas. */
export interface ContaFormulario {
  nome: string;
  valorPrevisto: number | null;
  descricao: string;
  categoriaId: number | null;
  tipo: TipoConta;
  vencimentoInicial: string;
  totalParcelas: number | null;
  parcelaAtual: number | null;
}
