import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { PaginaContasComponent } from './contas-page.component';
import { Compromisso, Conta } from './contas.models';
import {
  calcularContrasteHexadecimal,
  criarPaletaCategoria,
} from './contas-card-presentation';

describe('Tela de cadastro de contas', () => {
  let controladorHttp: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PaginaContasComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    controladorHttp = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controladorHttp.verify());

  it('carrega contas inativas e atualiza o filtro local sem alterar registros', () => {
    const fixture = TestBed.createComponent(PaginaContasComponent);
    fixture.detectChanges();
    controladorHttp.expectOne('/api/categorias?incluirInativas=true').flush([]);
    controladorHttp.expectOne((requisicao) =>
      requisicao.url === '/api/contas' && requisicao.params.get('incluirInativas') === 'true',
    ).flush([]);

    const contaAtiva = criarConta(1, true, 'parcelada');
    const contaInativa = criarConta(2, false, 'parcelada');
    const compromissosInativos = structuredClone(contaInativa.compromissos);
    const componente = fixture.componentInstance;
    componente.contas = [contaAtiva, contaInativa];
    componente.filtroTipo = 'parcelada';
    componente.filtroCategoria = '1';
    fixture.detectChanges();

    expect(componente.contasFiltradas.map((conta) => conta.id)).toEqual([1]);

    (fixture.nativeElement.querySelector('.inactive-filter input') as HTMLInputElement).click();
    fixture.detectChanges();
    expect(componente.incluirInativas).toBe(true);
    expect(componente.contasFiltradas.map((conta) => conta.id)).toEqual([1, 2]);
    expect(fixture.nativeElement.querySelector('.count')?.textContent?.trim()).toBe('2');
    expect(contaInativa.compromissos).toEqual(compromissosInativos);
  });

  it('prioriza atrasos pendentes e expande ou recolhe sem alterar estados', () => {
    const fixture = TestBed.createComponent(PaginaContasComponent);
    fixture.detectChanges();
    controladorHttp.expectOne('/api/categorias?incluirInativas=true').flush([]);
    controladorHttp.expectOne('/api/contas?incluirInativas=true').flush([]);

    const conta = criarConta(1, true, 'parcelada');
    conta.compromissos = [
      criarCompromisso(1, '2026-12-10', 'pendente', 'emDia'),
      criarCompromisso(2, '2026-11-01', 'pendente', 'atrasado'),
      criarCompromisso(3, '2026-10-01', 'pago', 'atrasado'),
      criarCompromisso(4, '2026-09-01', 'pendente', 'atrasado', true),
    ];
    const estadoOriginal = structuredClone(conta.compromissos);
    const componente = fixture.componentInstance;
    componente.contas = [conta];
    componente.categorias = [{ id: 1, nome: 'Moradia', cor: '#336699', ativa: true }];
    fixture.detectChanges();

    const linhas = fixture.nativeElement.querySelectorAll(
      '.commitment-sequence',
    ) as NodeListOf<Element>;
    expect(Array.from(linhas, (element) => element.textContent?.trim()))
      .toEqual(['Parcela 2', 'Parcela 1']);

    const botaoExpandir = fixture.nativeElement.querySelector(
      '.account-actions button',
    ) as HTMLButtonElement;
    expect(botaoExpandir.textContent).toContain('Ver todos os 4 compromissos');
    expect(fixture.nativeElement.querySelectorAll('.account-actions .account-action-buttons button'))
      .toHaveLength(2);
    botaoExpandir.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('.commitment-row')).toHaveLength(4);
    expect(fixture.nativeElement.textContent).toContain('Pago');
    expect(fixture.nativeElement.textContent).toContain('Cancelado');
    expect(
      fixture.nativeElement.querySelector('.account-actions button')?.textContent,
    ).toContain('Recolher (4)');
    expect(conta.compromissos).toEqual(estadoOriginal);

    (fixture.nativeElement.querySelector('.account-actions button') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.commitment-row')).toHaveLength(2);
    expect(conta.compromissos).toEqual(estadoOriginal);
  });

  it('mostra compromissos existentes quando nao ha pendencias ativas', () => {
    const fixture = TestBed.createComponent(PaginaContasComponent);
    fixture.detectChanges();
    controladorHttp.expectOne('/api/categorias?incluirInativas=true').flush([]);
    controladorHttp.expectOne('/api/contas?incluirInativas=true').flush([]);

    const conta = criarConta(1, true, 'unica');
    conta.compromissos = [
      criarCompromisso(1, '2026-12-10', 'pago', 'emDia'),
      criarCompromisso(2, '2026-11-01', 'pendente', 'atrasado', true),
      criarCompromisso(3, '2026-10-01', 'pago', 'atrasado'),
    ];

    expect(fixture.componentInstance.compromissosExibidos(conta).map((item) => item.sequencia))
      .toEqual([3, 2]);

    conta.compromissos = [criarCompromisso(1, '2026-12-10', 'pago', 'emDia')];
    expect(fixture.componentInstance.compromissosExibidos(conta)).toEqual(conta.compromissos);
  });

  it('gera cores dinamicas para a categoria e usa tons neutros sem cor', () => {
    const azul = criarPaletaCategoria('#336699');
    const verde = criarPaletaCategoria('#339966');
    const azulClaro = criarPaletaCategoria('#eaf6ff');
    const neutra = criarPaletaCategoria(null);
    const semCor = criarPaletaCategoria('');

    expect(azul['--cor-fundo-cartao']).not.toBe(verde['--cor-fundo-cartao']);
    expect(azul['--cor-fundo-cartao']).not.toBe('#336699');
    expect(azul['--cor-fundo-icone']).not.toBe(azul['--cor-fundo-cartao']);
    expect(azul['--cor-icone']).not.toBe('#336699');
    expect(
      calcularContrasteHexadecimal(azulClaro['--cor-icone'], azulClaro['--cor-fundo-icone']),
    ).toBeGreaterThanOrEqual(4.5);
    expect(neutra).toEqual(semCor);
    expect(neutra['--cor-fundo-cartao']).toBe('#ffffff');
  });

  it('mantem contraste de textos e controles para cores claras, escuras e saturadas', () => {
    for (const cor of ['#ffffff', '#000000', '#ff00ff', '#ffff00', '#0000ff', '#00ff00']) {
      const paleta = criarPaletaCategoria(cor);
      const fundo = paleta['--cor-fundo-cartao'];

      expect(calcularContrasteHexadecimal(paleta['--cor-texto-cartao'], fundo)).toBeGreaterThanOrEqual(7);
      expect(calcularContrasteHexadecimal(paleta['--cor-texto-secundario'], fundo))
        .toBeGreaterThanOrEqual(4.5);
      expect(
        calcularContrasteHexadecimal(paleta['--cor-texto-expandir'], paleta['--cor-fundo-expandir']),
      ).toBeGreaterThanOrEqual(4.5);
      expect(
        calcularContrasteHexadecimal(paleta['--cor-texto-editar'], paleta['--cor-fundo-editar']),
      ).toBeGreaterThanOrEqual(4.5);
      expect(calcularContrasteHexadecimal(paleta['--cor-borda-cartao'], fundo))
        .toBeGreaterThanOrEqual(1.3);
    }
  });

  it('mostra acoes alinhadas e preserva as cores semanticas dos compromissos', () => {
    const fixture = TestBed.createComponent(PaginaContasComponent);
    fixture.detectChanges();
    controladorHttp.expectOne('/api/categorias?incluirInativas=true').flush([]);
    controladorHttp.expectOne('/api/contas?incluirInativas=true').flush([]);

    const conta = criarConta(1, true, 'unica');
    conta.compromissos = [
      criarCompromisso(1, '2026-12-10', 'pendente', 'emDia'),
      criarCompromisso(2, '2026-11-10', 'pago', 'atrasado'),
    ];
    const componente = fixture.componentInstance;
    componente.contas = [conta];
    componente.categorias = [{ id: 1, nome: 'Moradia', cor: '#336699', ativa: true }];
    fixture.detectChanges();

    const acoes = fixture.nativeElement.querySelector('.account-actions') as HTMLElement;
    expect(acoes.querySelector('.account-action-leading')).not.toBeNull();
    expect(acoes.querySelector('.commitment-toggle')).toBeNull();
    expect(acoes.querySelector('.action-edit')?.textContent).toContain('Editar');
    expect(acoes.querySelector('.action-inactivate')?.textContent).toContain('Inativar');

    expect(fixture.nativeElement.querySelector('.badge-current')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.badge-paid')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.badge-pending')).not.toBeNull();
  });
});

function criarConta(id: number, ativa: boolean, tipo: Conta['tipo']): Conta {
  return {
    id,
    nome: `Conta ${id}`,
    valorPrevisto: 125,
    descricao: null,
    categoriaId: 1,
    categoriaNome: 'Moradia',
    tipo,
    vencimentoInicial: null,
    totalParcelas: tipo === 'parcelada' ? 4 : null,
    parcelaAtual: tipo === 'parcelada' ? 1 : null,
    ativa,
    compromissos: [criarCompromisso(1, '2026-12-10', 'pendente', 'emDia')],
  };
}

function criarCompromisso(
  sequencia: number,
  vencimento: string | null,
  situacaoPagamento: Compromisso['situacaoPagamento'],
  situacaoVencimento: Compromisso['situacaoVencimento'],
  cancelado = false,
): Compromisso {
  return {
    id: sequencia,
    sequencia,
    vencimento,
    valorPrevisto: 125,
    situacaoPagamento,
    situacaoVencimento,
    cancelado,
  };
}
