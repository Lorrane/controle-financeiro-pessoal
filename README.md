# Meu financeiro

Sistema web local para controle e planejamento financeiro pessoal. O projeto
está na etapa de estruturação: regras de negócio e cálculos só serão adicionados
depois de serem definidos e validados.

## Tecnologias

- **API:** C# e ASP.NET Core no .NET 10
- **Interface:** Angular com TypeScript e SCSS
- **Persistência:** SQLite com Entity Framework Core
- **Testes:** xUnit para o domínio e Vitest para a interface

## Estrutura

```text
src/
  ControleFinanceiro.Api/            API HTTP e configuração
  ControleFinanceiro.Application/    Casos de uso
  ControleFinanceiro.Domain/         Regras e modelos do domínio
  ControleFinanceiro.Infrastructure/ Persistência SQLite
tests/
  ControleFinanceiro.Domain.Tests/   Testes do domínio
frontend/                            Aplicação Angular
```

O domínio não depende das outras camadas. A aplicação depende do domínio; a
infraestrutura implementa persistência; a API conecta as camadas.

## Requisitos para desenvolvimento

- .NET 10 SDK
- Node.js e npm

## Executar localmente no Windows

Na raiz do projeto, em um terminal:

```powershell
dotnet run --project src\ControleFinanceiro.Api\ControleFinanceiro.Api.csproj --launch-profile http
```

Em outro terminal:

```powershell
Set-Location frontend
npm install
npm start
```

A interface fica em `http://localhost:4200`, e a verificação básica da API em
`http://localhost:5184/health`. A conexão SQLite está configurada para criar o
arquivo `controle-financeiro.db` quando a persistência for usada; o banco não é
criado até que existam entidades e migrations.

## Acesso pela rede Wi-Fi

Os perfis de rede local são opt-in. Para disponibilizar a interface no Wi-Fi,
execute `npm run start:lan`; para a API, use
`dotnet run --project src\ControleFinanceiro.Api\ControleFinanceiro.Api.csproj --launch-profile lan`.
Configure o endereço da interface na lista `Cors:AllowedOrigins` da API com o
IP local do computador e a porta `4200`. O firewall do Windows pode solicitar
permissão para a rede privada. Use somente em uma rede confiável: esta etapa
ainda não inclui autenticação e não deve ser exposta à internet.

## Testes

```powershell
dotnet test ControleFinanceiro.slnx
```

```powershell
Set-Location frontend
npm test -- --watch=false
```

Os testes de cálculos financeiros serão incluídos junto com as regras
correspondentes; não há regras financeiras presumidas nesta estrutura inicial.

## GitHub

O repositório está planejado como **público**: qualquer pessoa poderá ler o
código; apenas a conta proprietária terá permissão de escrita. Não publique
dados financeiros reais, arquivos SQLite, credenciais ou segredos.
