import { Compromisso, Conta } from './contas.models';

/** Propriedades CSS dinâmicas usadas para personalizar o cartão da conta. */
export type EstilosCartaoConta = Record<
  | '--cor-fundo-cartao'
  | '--cor-borda-cartao'
  | '--cor-fundo-icone'
  | '--cor-icone'
  | '--cor-texto-cartao'
  | '--cor-texto-secundario'
  | '--cor-divisoria-cartao'
  | '--cor-fundo-expandir'
  | '--cor-borda-expandir'
  | '--cor-texto-expandir'
  | '--cor-fundo-editar'
  | '--cor-borda-editar'
  | '--cor-texto-editar',
  string
>;

const paletaNeutra: EstilosCartaoConta = {
  '--cor-fundo-cartao': '#ffffff',
  '--cor-borda-cartao': '#e8ede9',
  '--cor-fundo-icone': '#edf5ef',
  '--cor-icone': '#39805a',
  '--cor-texto-cartao': '#27382e',
  '--cor-texto-secundario': '#52645a',
  '--cor-divisoria-cartao': '#e8ede9',
  '--cor-fundo-expandir': '#edf5ef',
  '--cor-borda-expandir': '#c9dccf',
  '--cor-texto-expandir': '#286c49',
  '--cor-fundo-editar': '#f0f7f2',
  '--cor-borda-editar': '#c9dccf',
  '--cor-texto-editar': '#286c49',
};

/**
 * Gera uma paleta suave e contrastante a partir da cor hexadecimal da categoria.
 * Cores ausentes ou inválidas usam o padrão neutro.
 * @param cor Cor no formato hexadecimal `#RRGGBB`, se configurada.
 * @returns Propriedades CSS para o cartão e seus controles.
 */
export function criarPaletaCategoria(cor: string | null): EstilosCartaoConta {
  const corBase = analisarCorHexadecimal(cor);
  if (!corBase) return { ...paletaNeutra };

  const fundoCartao = misturarComBranco(corBase, 0.82);
  const fundoIcone = misturarComBranco(corBase, 0.67);
  const corTexto = escurecerAteContraste(corBase, fundoCartao, 7);
  const corSecundaria = corComContrasteMinimo(
    misturarComBranco(corTexto, 0.14),
    fundoCartao,
    4.5,
  );
  const fundoExpandir = misturarComBranco(corBase, 0.76);
  const fundoEditar = misturarComBranco(corBase, 0.86);
  const textoExpandir = escurecerAteContraste(corBase, fundoExpandir);
  const textoEditar = escurecerAteContraste(corBase, fundoEditar);

  return {
    '--cor-fundo-cartao': formatarCor(fundoCartao),
    '--cor-borda-cartao': formatarCor(
      corComContrasteMinimo(misturarComBranco(corBase, 0.5), fundoCartao, 1.35),
    ),
    '--cor-fundo-icone': formatarCor(fundoIcone),
    '--cor-icone': formatarCor(escurecerAteContraste(corBase, fundoIcone)),
    '--cor-texto-cartao': formatarCor(corTexto),
    '--cor-texto-secundario': formatarCor(corSecundaria),
    '--cor-divisoria-cartao': formatarCor(
      corComContrasteMinimo(misturarComBranco(corBase, 0.58), fundoCartao, 1.25),
    ),
    '--cor-fundo-expandir': formatarCor(fundoExpandir),
    '--cor-borda-expandir': formatarCor(
      corComContrasteMinimo(misturarComBranco(corBase, 0.48), fundoExpandir, 1.35),
    ),
    '--cor-texto-expandir': formatarCor(textoExpandir),
    '--cor-fundo-editar': formatarCor(fundoEditar),
    '--cor-borda-editar': formatarCor(
      corComContrasteMinimo(misturarComBranco(corBase, 0.48), fundoEditar, 1.35),
    ),
    '--cor-texto-editar': formatarCor(textoEditar),
  };
}

/**
 * Calcula a razão de contraste WCAG entre duas cores hexadecimais.
 * @param corFrente Cor do texto ou controle.
 * @param corFundo Cor de fundo.
 * @returns Razão de contraste ou zero quando uma cor é inválida.
 */
export function calcularContrasteHexadecimal(corFrente: string, corFundo: string): number {
  const frente = analisarCorHexadecimal(corFrente);
  const fundo = analisarCorHexadecimal(corFundo);
  return frente && fundo ? contraste(frente, fundo) : 0;
}

/**
 * Prioriza pendências ativas, atrasos e datas antigas sem modificar a lista original.
 * @param conta Conta cujos compromissos serão ordenados.
 * @returns Nova lista ordenada; a lista persistida não é alterada.
 */
export function ordenarCompromissos(conta: Conta): Compromisso[] {
  return [...conta.compromissos].sort((primeiro, segundo) => {
    const prioridadePrimeiro = primeiro.situacaoPagamento === 'pendente' && !primeiro.cancelado ? 0 : 1;
    const prioridadeSegundo = segundo.situacaoPagamento === 'pendente' && !segundo.cancelado ? 0 : 1;
    if (prioridadePrimeiro !== prioridadeSegundo) return prioridadePrimeiro - prioridadeSegundo;

    if (prioridadePrimeiro === 0) {
      const atrasoPrimeiro = primeiro.situacaoVencimento === 'atrasado' ? 0 : 1;
      const atrasoSegundo = segundo.situacaoVencimento === 'atrasado' ? 0 : 1;
      if (atrasoPrimeiro !== atrasoSegundo) return atrasoPrimeiro - atrasoSegundo;
    }

    return compararDatas(primeiro, segundo) || primeiro.sequencia - segundo.sequencia;
  });
}

function analisarCorHexadecimal(cor: string | null): [number, number, number] | null {
  if (!cor || !/^#[\da-f]{6}$/i.test(cor)) return null;
  return [
    Number.parseInt(cor.slice(1, 3), 16),
    Number.parseInt(cor.slice(3, 5), 16),
    Number.parseInt(cor.slice(5, 7), 16),
  ];
}

function misturarComBranco(cor: [number, number, number], proporcaoBranco: number): [number, number, number] {
  const [vermelho, verde, azul] = cor;
  return [
    Math.round(vermelho * (1 - proporcaoBranco) + 255 * proporcaoBranco),
    Math.round(verde * (1 - proporcaoBranco) + 255 * proporcaoBranco),
    Math.round(azul * (1 - proporcaoBranco) + 255 * proporcaoBranco),
  ];
}

function escurecerAteContraste(
  cor: [number, number, number],
  fundo: [number, number, number],
  contrasteMinimo = 4.5,
): [number, number, number] {
  return corComContrasteMinimo(cor, fundo, contrasteMinimo);
}

function corComContrasteMinimo(
  cor: [number, number, number],
  fundo: [number, number, number],
  contrasteMinimo: number,
): [number, number, number] {
  const preto: [number, number, number] = [0, 0, 0];
  const branco: [number, number, number] = [255, 255, 255];
  const destino = contraste(preto, fundo) >= contraste(branco, fundo) ? preto : branco;
  let proporcao = 0.05;
  const misturar = (quantidade: number): [number, number, number] => {
    const [vermelho, verde, azul] = cor;
    const [vermelhoDestino, verdeDestino, azulDestino] = destino;
    return [
      Math.round(vermelho * (1 - quantidade) + vermelhoDestino * quantidade),
      Math.round(verde * (1 - quantidade) + verdeDestino * quantidade),
      Math.round(azul * (1 - quantidade) + azulDestino * quantidade),
    ];
  };
  let resultado = misturar(proporcao);

  while (contraste(resultado, fundo) < contrasteMinimo && proporcao < 1) {
    proporcao = Math.min(1, proporcao + 0.05);
    resultado = misturar(proporcao);
  }

  return resultado;
}

function contraste(
  primeiro: [number, number, number],
  segundo: [number, number, number],
): number {
  const luminancia = (cor: [number, number, number]): number => {
    const canais = cor.map((canal) => canal / 255).map((canal) =>
      canal <= 0.04045 ? canal / 12.92 : ((canal + 0.055) / 1.055) ** 2.4,
    );
    return canais[0] * 0.2126 + canais[1] * 0.7152 + canais[2] * 0.0722;
  };
  const valores = [luminancia(primeiro), luminancia(segundo)].sort((a, b) => b - a);
  return (valores[0] + 0.05) / (valores[1] + 0.05);
}

function formatarCor(cor: [number, number, number]): string {
  return `#${cor.map((canal) => canal.toString(16).padStart(2, '0')).join('')}`;
}

function compararDatas(primeiro: Compromisso, segundo: Compromisso): number {
  if (primeiro.vencimento === null) return segundo.vencimento === null ? 0 : 1;
  if (segundo.vencimento === null) return -1;
  return primeiro.vencimento.localeCompare(segundo.vencimento);
}
