# Guia de execução

## Pré-requisitos

- .NET 10 SDK.
- Node.js e npm.
- Windows PowerShell (os comandos abaixo usam caminhos compatíveis com Windows).

## Iniciar a aplicação

Na raiz do projeto, abra um terminal para a API:

```powershell
dotnet run --project src\ControleFinanceiro.Api\ControleFinanceiro.Api.csproj --launch-profile http
```

A API fica em `http://localhost:5184`. Na primeira execução, as migrations são
aplicadas e o banco `controle-financeiro.db` é criado no diretório de trabalho.
O endpoint `http://localhost:5184/health` verifica a disponibilidade da API.

Em outro terminal:

```powershell
Set-Location frontend
npm install
npm start
```

A interface fica em `http://localhost:4200`. O proxy local encaminha as
requisições `/api` para a API.

## Testes e builds

Na raiz:

```powershell
dotnet test ControleFinanceiro.slnx
dotnet build ControleFinanceiro.slnx
```

No diretório `frontend`:

```powershell
npm test -- --watch=false
npm run build
```

## Migrations

Instale uma ferramenta EF Core 10 compatível com a versão dos pacotes do
projeto e, na raiz, crie uma migration:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.11
dotnet ef migrations add NomeDaMigracao --project src\ControleFinanceiro.Infrastructure\ControleFinanceiro.Infrastructure.csproj --startup-project src\ControleFinanceiro.Api\ControleFinanceiro.Api.csproj --output-dir Persistence\Migrations
```

As migrations pendentes são aplicadas pela API na inicialização. Para inspecionar
um banco de desenvolvimento, pare a aplicação antes de substituir ou mover o
arquivo SQLite.

## Acesso em rede local

Os perfis de rede são opt-in:

```powershell
Set-Location frontend
npm run start:lan
```

Para a API, use o perfil `lan`:

```powershell
dotnet run --project src\ControleFinanceiro.Api\ControleFinanceiro.Api.csproj --launch-profile lan
```

Em uma rede privada confiável, acesse `http://<IP-do-computador>:4200`. Esta
versão não possui autenticação; não exponha o serviço à internet nem use dados
financeiros reais em demonstrações públicas.
