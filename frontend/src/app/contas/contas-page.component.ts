import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Categoria, Conta, ContaFormulario, SituacaoVencimento, TipoConta } from './contas.models';
import { ServicoDeContas } from './contas.service';
import {
  criarPaletaCategoria,
  EstilosCartaoConta,
  ordenarCompromissos,
} from './contas-card-presentation';

/** Cria o estado inicial independente para cadastro ou reinicialização do formulário. */
const formularioVazio = (): ContaFormulario => ({
  nome: '',
  valorPrevisto: null,
  descricao: '',
  categoriaId: null,
  tipo: 'unica',
  vencimentoInicial: '',
  totalParcelas: null,
  parcelaAtual: 1,
});

/** Apresenta contas, categorias, filtros e edição do módulo financeiro. */
@Component({
  imports: [CommonModule, FormsModule],
  selector: 'app-contas-page',
  templateUrl: './contas-page.component.html',
})
export class PaginaContasComponent implements OnInit {
  private readonly servicoContas = inject(ServicoDeContas);
  private readonly detectorDeAlteracoes = inject(ChangeDetectorRef);
  readonly moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
  readonly tipos: { valor: TipoConta; rotulo: string }[] = [
    { valor: 'unica', rotulo: 'Única' },
    { valor: 'parcelada', rotulo: 'Parcelada' },
    { valor: 'fixaMensal', rotulo: 'Fixa mensal' },
  ];
  readonly situacoes: { valor: SituacaoVencimento; rotulo: string }[] = [
    { valor: 'atrasado', rotulo: 'Atrasadas' },
    { valor: 'emDia', rotulo: 'Em dia' },
    { valor: 'semVencimento', rotulo: 'Sem vencimento' },
  ];

  contas: Conta[] = [];
  categorias: Categoria[] = [];
  formulario = formularioVazio();
  categoriaNome = '';
  categoriaCor = '#438565';
  categoriaEditandoId: number | null = null;
  editandoId: number | null = null;
  filtroTipo = '';
  filtroCategoria = '';
  filtroSituacao = '';
  incluirInativas = false;
  mostrarCategorias = false;
  carregando = false;
  salvando = false;
  erro = '';
  aviso = '';
  cartoesExpandidos = new Set<number>();
  readonly ajudasAbertas: Record<string, boolean> = {};

  /** Carrega os dados iniciais para os filtros e os formulários da tela. */
  ngOnInit(): void {
    this.carregar();
  }

  /** Aplica filtros de situação, categoria, tipo e inativação sem alterar os registros. */
  get contasFiltradas(): Conta[] {
    return this.contas.filter((conta) => {
      if (!this.incluirInativas && !conta.ativa) return false;
      if (this.filtroTipo && conta.tipo !== this.filtroTipo) return false;
      if (this.filtroCategoria && conta.categoriaId !== Number(this.filtroCategoria)) return false;
      if (
        this.filtroSituacao &&
        !conta.compromissos.some(
          (compromisso) =>
            !compromisso.cancelado && compromisso.situacaoVencimento === this.filtroSituacao,
        )
      ) {
        return false;
      }
      return true;
    });
  }

  /** Retorna apenas categorias disponíveis para novos cadastros e edições. */
  get categoriasAtivas(): Categoria[] {
    return this.categorias.filter((categoria) => categoria.ativa);
  }

  /** Obtém os tons visuais da categoria já carregada, com uma paleta neutra de reserva. */
  estilosDoCartao(conta: Conta): EstilosCartaoConta {
    return criarPaletaCategoria(
      this.categorias.find((categoria) => categoria.id === conta.categoriaId)?.cor ?? null,
    );
  }

  /** Exibe no máximo dois compromissos, priorizando pendências e atrasos. */
  compromissosExibidos(conta: Conta): Conta['compromissos'] {
    const compromissos = ordenarCompromissos(conta);
    return this.cartoesExpandidos.has(conta.id) ? compromissos : compromissos.slice(0, 2);
  }

  /** Alterna apenas a apresentação dos compromissos, sem modificar os dados da conta. */
  alternarCompromissos(conta: Conta): void {
    if (this.cartoesExpandidos.has(conta.id)) {
      this.cartoesExpandidos.delete(conta.id);
    } else {
      this.cartoesExpandidos.add(conta.id);
    }
    this.detectorDeAlteracoes.markForCheck();
  }

  /** Abre ou fecha a ajuda contextual identificada pelo campo informado. */
  alternarAjuda(campo: string): void {
    this.ajudasAbertas[campo] = !this.ajudasAbertas[campo];
    this.detectorDeAlteracoes.markForCheck();
  }

  /** Informa se o formulário está configurado para parcelamento. */
  get ehParcelada(): boolean {
    return this.formulario.tipo === 'parcelada';
  }

  /** Informa se o tipo selecionado exige uma data de vencimento. */
  get exigeVencimento(): boolean {
    return this.formulario.tipo !== 'unica';
  }

  /** Atualiza categorias e carrega contas ativas e inativas para filtragem local. */
  carregar(): void {
    this.carregando = true;
    this.erro = '';
    this.servicoContas.listarCategorias(true).subscribe({
      next: (categorias) => {
        this.categorias = categorias;
        this.detectorDeAlteracoes.markForCheck();
      },
      error: (erroRecebido: unknown) => this.mostrarErro(erroRecebido),
    });
    this.servicoContas.listarContas(true).subscribe({
      next: (contas) => {
        this.contas = contas;
        this.carregando = false;
        this.detectorDeAlteracoes.markForCheck();
      },
      error: (erroRecebido: unknown) => {
        this.carregando = false;
        this.mostrarErro(erroRecebido);
        this.detectorDeAlteracoes.markForCheck();
      },
    });
  }

  /** Valida e cria ou atualiza a conta selecionada no formulário. */
  salvarConta(): void {
    this.erro = '';
    this.aviso = '';
    if (!this.validarFormulario()) return;

    this.salvando = true;
    const comuns = {
      nome: this.formulario.nome.trim(),
      valorPrevisto: Number(this.formulario.valorPrevisto),
      descricao: this.formulario.descricao.trim() || null,
      categoriaId: Number(this.formulario.categoriaId),
      vencimentoInicial: this.formulario.vencimentoInicial || null,
    };

    const requisicao =
      this.editandoId === null
        ? this.servicoContas.criarConta({
            ...comuns,
            tipo: this.formulario.tipo,
            totalParcelas: this.ehParcelada ? Number(this.formulario.totalParcelas) : null,
            parcelaAtual: this.ehParcelada ? Number(this.formulario.parcelaAtual) : null,
          })
        : this.servicoContas.atualizarConta(this.editandoId, comuns);

    requisicao.subscribe({
      next: () => {
        this.salvando = false;
        this.aviso = this.editandoId === null ? 'Conta cadastrada.' : 'Alterações salvas.';
        this.cancelarEdicao();
        this.carregar();
        this.detectorDeAlteracoes.markForCheck();
      },
      error: (erroRecebido: unknown) => {
        this.salvando = false;
        this.mostrarErro(erroRecebido);
        this.detectorDeAlteracoes.markForCheck();
      },
    });
  }

  /** Carrega uma conta ativa no formulário de edição. */
  editarConta(conta: Conta): void {
    if (!conta.ativa) return;
    this.editandoId = conta.id;
    this.formulario = {
      nome: conta.nome,
      valorPrevisto: conta.valorPrevisto,
      descricao: conta.descricao ?? '',
      categoriaId: conta.categoriaId,
      tipo: conta.tipo,
      vencimentoInicial: conta.vencimentoInicial ?? '',
      totalParcelas: conta.totalParcelas,
      parcelaAtual: conta.parcelaAtual,
    };
    this.erro = '';
    this.aviso = '';
    document.getElementById('form-conta')?.scrollIntoView({ behavior: 'smooth' });
  }

  /** Descarta os valores temporários e encerra a edição da conta. */
  cancelarEdicao(): void {
    this.editandoId = null;
    this.formulario = formularioVazio();
  }

  /** Solicita confirmação antes de inativar uma conta e preservar seu histórico. */
  inativarConta(conta: Conta): void {
    if (!window.confirm(`Inativar "${conta.nome}"? Os compromissos e históricos serão preservados.`)) {
      return;
    }

    this.servicoContas.inativarConta(conta.id).subscribe({
      next: () => {
        this.aviso = 'Conta inativada; os compromissos foram preservados.';
        this.carregar();
        this.detectorDeAlteracoes.markForCheck();
      },
      error: (erroRecebido: unknown) => this.mostrarErro(erroRecebido),
    });
  }

  /** Valida e cria ou atualiza a categoria em edição. */
  salvarCategoria(): void {
    this.erro = '';
    const nome = this.categoriaNome.trim();
    if (!nome || nome.length > 80) {
      this.erro = 'Informe um nome de categoria com até 80 caracteres.';
      return;
    }

    const requisicao =
      this.categoriaEditandoId === null
        ? this.servicoContas.criarCategoria(nome, this.categoriaCor || null)
        : this.servicoContas.atualizarCategoria(this.categoriaEditandoId, nome, this.categoriaCor || null);

    requisicao.subscribe({
      next: () => {
        this.categoriaNome = '';
        this.categoriaCor = '#438565';
        this.categoriaEditandoId = null;
        this.aviso = 'Categoria salva.';
        this.carregar();
        this.detectorDeAlteracoes.markForCheck();
      },
      error: (erroRecebido: unknown) => this.mostrarErro(erroRecebido),
    });
  }

  /** Carrega uma categoria ativa no formulário de edição. */
  editarCategoria(categoria: Categoria): void {
    this.categoriaEditandoId = categoria.id;
    this.categoriaNome = categoria.nome;
    this.categoriaCor = categoria.cor ?? '#438565';
  }

  /** Descarta alterações não salvas da categoria em edição. */
  cancelarCategoria(): void {
    this.categoriaEditandoId = null;
    this.categoriaNome = '';
    this.categoriaCor = '#438565';
  }

  /** Solicita confirmação antes de inativar uma categoria. */
  inativarCategoria(categoria: Categoria): void {
    if (!window.confirm(`Inativar a categoria "${categoria.nome}"?`)) return;
    this.servicoContas.inativarCategoria(categoria.id).subscribe({
      next: () => {
        this.aviso = 'Categoria inativada.';
        this.carregar();
        this.detectorDeAlteracoes.markForCheck();
      },
      error: (erroRecebido: unknown) => this.mostrarErro(erroRecebido),
    });
  }

  /** Formata uma data ISO como data local brasileira, sem conversão de fuso. */
  formatarData(data: string | null): string {
    if (!data) return 'Sem vencimento';
    const [ano, mes, dia] = data.split('-').map(Number);
    return new Intl.DateTimeFormat('pt-BR').format(new Date(ano, mes - 1, dia));
  }

  /** Traduz o tipo de conta para o rótulo exibido na interface. */
  rotuloTipo(tipo: TipoConta): string {
    return this.tipos.find((item) => item.valor === tipo)?.rotulo ?? tipo;
  }

  /** Traduz a classificação de vencimento para texto em português. */
  rotuloSituacao(situacao: SituacaoVencimento): string {
    return {
      atrasado: 'Atrasado',
      emDia: 'Em dia',
      semVencimento: 'Sem vencimento',
    }[situacao];
  }

  private validarFormulario(): boolean {
    if (!this.formulario.nome.trim() || this.formulario.nome.trim().length > 120) {
      this.erro = 'Informe um nome com até 120 caracteres.';
      return false;
    }
    if (this.formulario.valorPrevisto === null || !Number.isFinite(Number(this.formulario.valorPrevisto)) || Number(this.formulario.valorPrevisto) <= 0) {
      this.erro = 'O valor previsto precisa ser maior que zero.';
      return false;
    }
    if (!this.formulario.categoriaId) {
      this.erro = 'Selecione uma categoria ativa.';
      return false;
    }
    if (this.exigeVencimento && !this.formulario.vencimentoInicial) {
      this.erro = 'Informe o vencimento para este tipo de conta.';
      return false;
    }
    if (this.ehParcelada) {
      const total = Number(this.formulario.totalParcelas);
      const atual = Number(this.formulario.parcelaAtual);
      if (!Number.isInteger(total) || total < 1 || !Number.isInteger(atual) || atual < 1 || atual > total) {
        this.erro = 'A parcela atual deve ser um número entre 1 e o total de parcelas.';
        return false;
      }
    }
    return true;
  }

  private mostrarErro(erroRecebido: unknown): void {
    if (erroRecebido instanceof HttpErrorResponse) {
      this.erro =
        typeof erroRecebido.error?.erro === 'string'
          ? erroRecebido.error.erro
          : typeof erroRecebido.error?.title === 'string'
            ? erroRecebido.error.title
          : erroRecebido.status === 0
            ? 'Não foi possível conectar à API. Verifique se ela está em execução.'
            : `Não foi possível concluir a operação (HTTP ${erroRecebido.status}).`;
    } else {
      this.erro = 'Ocorreu um erro inesperado ao carregar os dados.';
    }
    this.detectorDeAlteracoes.markForCheck();
  }
}
