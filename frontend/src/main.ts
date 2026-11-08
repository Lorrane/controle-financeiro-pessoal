import { bootstrapApplication } from '@angular/platform-browser';
import { configuracaoAplicacao } from './app/app.config';
import { Aplicacao } from './app/app';

/** Inicializa o componente raiz e registra erros de inicialização no console. */
bootstrapApplication(Aplicacao, configuracaoAplicacao)
  .catch((erro: unknown) => console.error(erro));
