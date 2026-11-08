import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Categoria, Conta } from './contas.models';

/** Centraliza as chamadas HTTP do módulo de contas e categorias. */
@Injectable({ providedIn: 'root' })
export class ServicoDeContas {
  private readonly clienteHttp = inject(HttpClient);
  private readonly urlContas = '/api/contas';
  private readonly urlCategorias = '/api/categorias';

  /** Lista contas ativas e inativas para que a tela aplique os filtros localmente. */
  listarContas(incluirInativas = true): Observable<Conta[]> {
    return this.clienteHttp.get<Conta[]>(this.urlContas, {
      params: incluirInativas ? { incluirInativas: true } : {},
    });
  }

  /** Cadastra uma conta e recebe os compromissos iniciais gerados pela API. */
  criarConta(conta: Omit<Conta, 'id' | 'categoriaNome' | 'ativa' | 'compromissos'>): Observable<Conta> {
    return this.clienteHttp.post<Conta>(this.urlContas, conta);
  }

  /** Atualiza os dados editáveis de uma conta existente. */
  atualizarConta(
    id: number,
    conta: Pick<Conta, 'nome' | 'valorPrevisto' | 'descricao' | 'categoriaId' | 'vencimentoInicial'>,
  ): Observable<Conta> {
    return this.clienteHttp.put<Conta>(`${this.urlContas}/${id}`, conta);
  }

  /** Solicita a inativação sem exclusão física da conta. */
  inativarConta(id: number): Observable<void> {
    return this.clienteHttp.delete<void>(`${this.urlContas}/${id}`);
  }

  /** Lista categorias e inclui as inativas quando solicitado. */
  listarCategorias(incluirInativas = false): Observable<Categoria[]> {
    return this.clienteHttp.get<Categoria[]>(this.urlCategorias, {
      params: incluirInativas ? { incluirInativas: true } : {},
    });
  }

  /** Cadastra uma categoria com cor opcional. */
  criarCategoria(nome: string, cor: string | null): Observable<Categoria> {
    return this.clienteHttp.post<Categoria>(this.urlCategorias, { nome, cor });
  }

  /** Atualiza o nome e a cor de uma categoria. */
  atualizarCategoria(id: number, nome: string, cor: string | null): Observable<Categoria> {
    return this.clienteHttp.put<Categoria>(`${this.urlCategorias}/${id}`, { nome, cor });
  }

  /** Solicita a inativação sem apagar contas ou compromissos associados. */
  inativarCategoria(id: number): Observable<void> {
    return this.clienteHttp.delete<void>(`${this.urlCategorias}/${id}`);
  }
}
