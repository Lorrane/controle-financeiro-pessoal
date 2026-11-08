import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { rotas } from './app.routes';

/** Configuração global de provedores da aplicação Angular. */
export const configuracaoAplicacao: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(rotas),
    provideHttpClient(),
  ]
};
