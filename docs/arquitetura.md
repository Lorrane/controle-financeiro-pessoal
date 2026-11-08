# Arquitetura

## Visão geral

O Meu Financeiro separa regras financeiras, casos de uso, persistência e
interface. A solução evita dependências arquiteturais adicionais e mantém o
domínio independente de ASP.NET Core, Entity Framework Core e Angular.

```text
Angular ── HTTP /api ──> API ASP.NET Core
                              │
                         Application
                              │
                           Domain
                              ▲
                       Infrastructure
                              │
                         SQLite / EF Core
```

## Responsabilidades

- `ControleFinanceiro.Domain`: categorias, contas, compromissos, validações e
  regras de geração e cancelamento.
- `ControleFinanceiro.Application`: contratos de entrada e saída e serviços
  que coordenam as operações do módulo.
- `ControleFinanceiro.Infrastructure`: repositórios, mapeamentos do EF Core,
  conversões SQLite e migrations.
- `ControleFinanceiro.Api`: endpoints REST, validação HTTP, configuração e
  inicialização das migrations.
- `frontend/src/app/contas`: modelos da interface, chamadas HTTP, apresentação
  e filtros da listagem.
- `tests/ControleFinanceiro.Domain.Tests`: testes das regras, serviços e
  persistência.

## Persistência e fluxo

`Conta` pertence a uma `CategoriaConta` e possui vários `Compromisso`. Cada
compromisso guarda número sequencial, vencimento, valor previsto, estado de
pagamento, cancelamento e registros de alteração. Os compromissos não são
removidos quando a programação da conta muda ou ela é inativada.

As consultas da API podem renovar ocorrências mensais de contas ativas. A
geração é limitada ao horizonte móvel de 12 meses e protegida por uma chave
única por conta e sequência. Não há serviço em segundo plano.

O SQLite armazena valores `decimal` como texto decimal invariável e vencimentos
como datas sem horário. A inicialização da API aplica as migrations pendentes.
No frontend, `/api` é encaminhado pelo proxy de desenvolvimento configurado em
`frontend/proxy.conf.json`.

## Limites desta etapa

O módulo não registra pagamentos, movimentações de caixa, metas ou relatórios.
O status de pagamento existe na modelagem, mas nenhum fluxo desta etapa altera
um compromisso para pago.
