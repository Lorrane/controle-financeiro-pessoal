import { Component } from '@angular/core';
import { PaginaContasComponent } from './contas/contas-page.component';

/** Componente raiz que hospeda o módulo atual de cadastro de contas. */
@Component({
  imports: [PaginaContasComponent],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class Aplicacao {
  protected readonly tituloAplicacao = 'Meu Financeiro';
}
