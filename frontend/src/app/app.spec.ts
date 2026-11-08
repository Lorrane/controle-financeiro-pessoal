import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Aplicacao } from './app';
import { PaginaContasComponent } from './contas/contas-page.component';

describe('Aplicacao', () => {
  let controladorHttp: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Aplicacao],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    })
      .compileComponents();
    controladorHttp = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    controladorHttp.verify();
  });

  it('cria o componente raiz da aplicacao', () => {
    const componenteDeTeste = TestBed.createComponent(Aplicacao);
    componenteDeTeste.detectChanges();
    controladorHttp.expectOne('/api/categorias?incluirInativas=true').flush([]);
    controladorHttp.expectOne('/api/contas?incluirInativas=true').flush([]);
    const aplicacao = componenteDeTeste.componentInstance;
    expect(aplicacao).toBeTruthy();
  });

  it('exibe a tela de cadastro de contas', async () => {
    const componenteDeTeste = TestBed.createComponent(Aplicacao);
    componenteDeTeste.detectChanges();
    controladorHttp.expectOne('/api/categorias?incluirInativas=true').flush([]);
    controladorHttp.expectOne('/api/contas?incluirInativas=true').flush([]);
    await componenteDeTeste.whenStable();
    const componenteRenderizado = componenteDeTeste.nativeElement as HTMLElement;
    expect(componenteRenderizado.querySelector('h1')?.textContent).toContain('Cadastro de contas');
    expect(componenteRenderizado.textContent).toContain('Adicionar conta');
  });

  it('exige vencimento para conta mensal antes de enviar a requisicao', () => {
    const componenteDeTeste = TestBed.createComponent(PaginaContasComponent);
    componenteDeTeste.detectChanges();
    controladorHttp.expectOne('/api/categorias?incluirInativas=true').flush([]);
    controladorHttp.expectOne('/api/contas?incluirInativas=true').flush([]);

    componenteDeTeste.componentInstance.categorias = [{ id: 1, nome: 'Moradia', cor: null, ativa: true }];
    componenteDeTeste.componentInstance.formulario = {
      nome: 'Aluguel',
      valorPrevisto: 1200,
      descricao: '',
      categoriaId: 1,
      tipo: 'fixaMensal',
      vencimentoInicial: '',
      totalParcelas: null,
      parcelaAtual: null,
    };
    componenteDeTeste.componentInstance.salvarConta();

    expect(componenteDeTeste.componentInstance.erro).toContain('vencimento');
    controladorHttp.expectNone('/api/contas');
  });
});
