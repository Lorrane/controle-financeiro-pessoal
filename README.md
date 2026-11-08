# Meu Financeiro

Aplicação web para organizar categorias, contas e compromissos financeiros
pessoais. O projeto está **em desenvolvimento** e esta primeira etapa implementa
somente o cadastro e a organização dos compromissos previstos.

> Pagamentos, movimentações de caixa, metas, previsões e relatórios são
> funcionalidades futuras. Não estão implementadas nesta versão.

## Tecnologias

- **Backend:** C#, ASP.NET Core e .NET 10.
- **Frontend:** Angular, TypeScript e SCSS.
- **Persistência:** SQLite e Entity Framework Core 10.
- **Testes:** xUnit e Vitest.

## Arquitetura e estrutura

A solução separa domínio, casos de uso, persistência e API. O domínio não
depende das outras camadas; a infraestrutura implementa persistência, e a API
expõe os casos de uso por HTTP.

```text
src/
  ControleFinanceiro.Api/            API HTTP e inicialização
  ControleFinanceiro.Application/    Casos de uso e contratos
  ControleFinanceiro.Domain/         Entidades e regras de negócio
  ControleFinanceiro.Infrastructure/ Persistência SQLite e migrations
tests/
  ControleFinanceiro.Domain.Tests/   Testes de domínio, aplicação e persistência
frontend/
  src/app/                           Aplicação Angular e tela de contas
docs/                                Arquitetura, regras e guia de execução
```

Documentação detalhada:

- [Arquitetura](docs/arquitetura.md)
- [Regras de negócio](docs/regras-de-negocio.md)
- [Guia de execução](docs/guia-de-execucao.md)

## Funcionalidades implementadas

- Cadastro, edição e inativação de categorias com cor opcional.
- Cadastro, edição, listagem e inativação de contas únicas, parceladas e fixas
  mensais.
- Filtros de contas por tipo, categoria, situação de vencimento e estado ativo.
- Compromissos individualizados, numerados e vinculados à conta original.
- Geração inicial de parcelas a partir da parcela atual e geração recorrente
  limitada a um horizonte móvel de 12 meses.
- Preservação de histórico nas alterações; situação de pagamento independente
  da classificação de vencimento.
- Interface responsiva com ajuda contextual e temas derivados das categorias.

Nenhum compromisso é marcado como pago automaticamente. Não há nesta etapa
fluxo de pagamentos ou de movimentação financeira.

## Pré-requisitos

- .NET 10 SDK.
- Node.js e npm.

## Executar localmente

Na raiz do projeto, inicie a API:

```powershell
dotnet run --project src\ControleFinanceiro.Api\ControleFinanceiro.Api.csproj --launch-profile http
```

Em outro terminal, instale dependências e inicie a interface:

```powershell
Set-Location frontend
npm install
npm start
```

A interface estará em `http://localhost:4200`; a API, em
`http://localhost:5184`. O proxy de desenvolvimento encaminha `/api` à API.
`http://localhost:5184/health` verifica sua disponibilidade.

## SQLite e migrations

A cadeia de conexão de desenvolvimento fica em
`src/ControleFinanceiro.Api/appsettings.json`. Por padrão, a API cria
`controle-financeiro.db` no diretório de trabalho e aplica migrations pendentes
ao iniciar. Arquivos SQLite e seus auxiliares locais são ignorados pelo Git.

Para criar uma migration com EF Core 10:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.11
dotnet ef migrations add NomeDaMigracao --project src\ControleFinanceiro.Infrastructure\ControleFinanceiro.Infrastructure.csproj --startup-project src\ControleFinanceiro.Api\ControleFinanceiro.Api.csproj --output-dir Persistence\Migrations
```

## Testes e builds

Na raiz da solução:

```powershell
dotnet test ControleFinanceiro.slnx
dotnet build ControleFinanceiro.slnx
```

No diretório `frontend`:

```powershell
npm test -- --watch=false
npm run build
```

## Acesso na rede local

Para testar em um celular conectado à mesma rede Wi-Fi, execute a interface
com `npm run start:lan` e a API com o perfil `lan`. Acesse no celular
`http://<IP-do-computador>:4200`. O [guia de execução](docs/guia-de-execucao.md)
detalha os comandos.

Esta versão não possui autenticação. Use apenas em uma rede confiável e não a
exponha à internet.

## Segurança e portfólio

O repositório pode ser público para apresentação como portfólio. Não publique
dados financeiros pessoais, bancos SQLite, arquivos de ambiente com segredos,
tokens, senhas ou chaves. A configuração versionada contém somente valores de
desenvolvimento sem credenciais.
