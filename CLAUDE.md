# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Arauco Otimizador API** is a serverless microservices application built with ASP.NET Core (.NET 10.0) running on AWS Lambda. This repository is used as a .NET base project containing reusable structures and existing implementations that can be adapted for new projects.

The current code still carries domain-specific implementations (Auth, Cartão, Conta, Data API, Flow/WebHook, Functions, etc.) that are planned to be reviewed and removed in a later cleanup. The base infrastructure, common libraries, and AWS helpers are kept intact.

## Build and Run Commands

```bash
# Build entire solution
dotnet build arauco-otimizador-api.sln --configuration Debug

# Run a WebApi locally (uses LocalEntryPoint.cs with Kestrel)
cd Arauco.Otimizador.WebApi.AuthApi
dotnet run

# Build for release/deployment
dotnet build arauco-otimizador-api.sln --configuration Release

# Install Lambda deployment tools
dotnet tool install -g Amazon.Lambda.Tools

# Deploy individual API via SAM (requires AWS credentials)
dotnet lambda deploy-serverless --configuration Release --framework net10.0 --region sa-east-1 --template serverless.yml
```

## Architecture

### Layered Structure
```
Controllers (WebApi) → Services → UnitOfWork → Repositories → DbContext → MySQL
```

**Project Organization:**
- `Arauco.Otimizador.Common.*` - Domain models, enums, events, session, email/pdf helpers
- `Arauco.Otimizador.Data.*` - Entity definitions, EF Core DbContexts, repositories, DynamoDB docs
- `Arauco.Otimizador.Service.*` - Business logic layer, all inherit from `ServiceBase`
- `Arauco.Otimizador.WebApi.*` - HTTP API endpoints as Lambda functions
- `Arauco.Otimizador.Function.*` - Event-driven Lambda functions (email, Cognito triggers, etc.)
- `Arauco.Otimizador.Aws.*` - AWS service integrations (CloudFront, shared resources)
- `Techer.Common.*` - Company-wide reusable common libraries (kept unchanged)
- `Techer.Aws.*` - Company-wide reusable AWS helpers (kept unchanged)
- `Techer.Data.MySql` - Generic MySQL repository and DbContext utilities (kept unchanged)

### Key Patterns

**Unit of Work:** `IUnitOfWork` manages transactions with generic repositories
```csharp
await unitOfWork.CartaoRepository.Add(entity);
await unitOfWork.SaveAsync();
```

**Service Base Class:** All services inherit from `ServiceBase` with injected `IUnitOfWork` and `IEnvironmentVariables`

**Dual Entry Points:** Each WebApi has `LambdaEntryPoint.cs` (AWS) and `LocalEntryPoint.cs` (local development)

**Environment Variables:** Access via `IEnvironmentVariables` interface with `IsLocal()`, `IsDevelopment()`, `IsProduction()` methods

### Database Contexts
- `DbContext` (`Arauco.Otimizador.Data.MySql`) - single EF Core context for all product entities (Cartao, Cenario, Parametro, Demanda, Pedido, etc.)

Database credentials come from AWS Secrets Manager via `UseMySqlWithSecrets()` extension.

## Guia para Novos Recursos (onde implementar cada coisa)

Este projeto é usado como base para múltiplos produtos. Ao adicionar um novo recurso de domínio
(ex.: um novo "Cartão", "Cenário", "Pedido" etc.), siga sempre a mesma estrutura em camadas,
usando o recurso **`Cartao`** (implementação original/base) e o recurso **`Cenario`/`Parametro`/`Demanda`/`Pedido`**
(módulo "Otimizador de Pedidos", adicionado seguindo este mesmo guia) como referência. **Nunca remova
a implementação do `Cartao`** — ela é o exemplo canônico mantido de propósito no projeto.

**Regra importante sobre a camada de API**: existe apenas **um** projeto de API de produto,
`Arauco.Otimizador.WebApi` (pasta física `Arauco.Otimizador.WebApi.Cartao`), além da biblioteca base
`Arauco.Otimizador.WebApi.Base`. Cada novo recurso ganha uma **nova controller dentro do mesmo
projeto** (`Controllers/<Recurso>Controller.cs`) — **não** crie um novo projeto/Lambda por tipo de
recurso. Veja o item 8 abaixo para detalhes.

Para cada novo recurso, crie/edite os seguintes arquivos, um por camada:

1. **Enums** — `Arauco.Otimizador.Common.Domain/Enums/<Recurso>/<Nome>Enum.cs`
   Enums que descrevem valores fechados do recurso (status, tipo, etc.), ex.:
   `Enums/Cartao/CartaoTipoEnum.cs`, `Enums/Cenario/StatusCenarioEnum.cs`. Se um valor precisar de um
   texto diferente do nome do member em C#, use `[EnumMember(Value = "...")]` por item — veja
   `Enums/Cenario/StatusCenarioEnum.cs`. **Não** decore o enum em si com `[JsonConverter(typeof(StringEnumConverter))]`
   — isso forçaria serialização em string em todo lugar que o enum for usado (entidades EF incluídas).
   Em vez disso, aplique `[JsonConverter(typeof(StringEnumConverter))]` (Newtonsoft) na **propriedade**
   do Model (`*Request`/`*Response`) que realmente precisa desse parse, ex.: `CenarioListaResponse.Status`,
   `PedidoListaResponse.TipoFrete` — veja `Models/Cenario/CenarioListaResponse.cs`.

2. **Modelos de comunicação (DTOs)** — `Arauco.Otimizador.Common.Domain/Models/<Recurso>/*.cs`
   **Regra: um modelo de Request/Response por operação, nunca um modelo genérico reaproveitado entre
   operações diferentes.** Não crie um `<Recurso>Model`/`<Recurso>Response` único com todos os campos
   (vários deles `nullable` só porque nem toda operação os preenche) para servir list/get/criar/atualizar
   ao mesmo tempo — isso é exatamente o que **não** fazer. Em vez disso, nomeie por
   `<Recurso><Operação><Request|Response>` e inclua em cada um **apenas os campos relevantes daquela
   operação**:
   - `CenarioListaResponse` (`GET /Cenarios`) — sem a lista de `Parametros` (pesada, só cabe no detalhe).
   - `CenarioDetalheResponse` (`GET /Cenarios/{id}`) — estado completo.
   - `CenarioCriacaoRequest`/`CenarioCriacaoResponse` (`POST /Cenarios`) — a response aqui **não**
     inclui `ArquivoNome`/`DataUltimoProcessamento`/`Submetido`/`PrimeiraSemana`/`UltimaSemana`, porque
     logo após criar um cenário esses campos são sempre nulos/vazios/false — não fazem parte do que é
     necessário saber naquele momento.
   - `CenarioUploadArquivoResponse`, `CenarioProcessamentoResponse`, `CenarioSubmissaoResponse` — cada
     ação (`POST /{id}/csv`, `/processar`, `/submeter`) tem sua própria response, mesmo que o formato
     acabe idêntico ao do detalhe (ver `Models/Cenario/*.cs` para os exemplos completos).

   **Exceção — objetos de valor aninhados**: um sub-objeto pequeno, simétrico e sem campos que variam
   por operação (ex.: `SemanaAnoResponse { Ano, Semana }`, `ParametroValorRequest`/`ParametroValorResponse
   { Valor, Rotulo, Peso }`) pode continuar compartilhado entre modelos — o problema que essa regra
   evita é o de um **modelo de recurso de topo** genérico demais, não o de um objeto de valor sem
   ambiguidade nenhuma. Da mesma forma, é normal um `<Recurso>ListaResponse` de um recurso ser reusado
   como formato "resumo" embutido dentro do Response de **outro** recurso (ex.: `CenarioDetalheResponse.Parametros`
   é `List<ParametroListaResponse>`) — isso é composição entre recursos diferentes, não o mesmo problema
   de uma operação reaproveitando o modelo de outra operação do mesmo recurso.

   Para recursos internos simples como o Cartão, o mesmo princípio já era seguido com outra convenção de
   nomes (`CartaoListaModel`, `CartaoDetalheModel`, `CartaoNovoModel` — um modelo por operação, só que
   com sufixo `Model` em vez de `Request`/`Response`); mantenha o padrão de nomes que já estiver em uso
   no recurso/domínio em questão, mas sempre com um tipo por operação.

3. **Interface do serviço** — `Arauco.Otimizador.Common.Domain/Services/<Recurso>/I<Recurso>Service.cs`
   Contrato do serviço, com um método async por operação de negócio (`ListarAsync`, `ObterAsync`,
   `CriarAsync`, etc.). Toda a lógica de negócio é acessada pelas camadas superiores **somente**
   através dessa interface (DI), nunca pela classe concreta.

4. **Entidades do EF Core** — `Arauco.Otimizador.Data.Entities/<Recurso>/*.cs`
   Classes simples (POCO), sem navegações complexas — o padrão do projeto é usar apenas chaves
   estrangeiras como colunas simples (ex.: `int`/`string` Id) e resolver relacionamentos via LINQ
   manual nos serviços, não via navigation properties do EF. Veja `Data.Entities/Cartao/Cartao.cs` e
   `Data.Entities/Cenario/Cenario.cs`.

5. **UnitOfWork** — `Arauco.Otimizador.Data.Entities/IUnitOfWork.cs` (interface) e
   `Arauco.Otimizador.Data.MySql/UnitOfWork.cs` (implementação)
   Adicione uma propriedade `IGenericRepository<SuaEntidade> SuaEntidadeRepository { get; }` na
   interface, e a implementação lazy-init correspondente na classe concreta (copie o padrão usado
   para `CartaoRepository`/`CenarioRepository`). `IGenericRepository<T>` (`Techer.Data.MySql`) já
   cobre as operações CRUD comuns (`Where`, `FirstOrDefaultAsync`, `Add`, `AddRange`, `Remove`,
   `RemoveRange`, `AnyAsync`, etc.) — não crie repositórios especializados.

6. **DbContext** — `Arauco.Otimizador.Data.MySql/DbContext.cs`
   Adicione o `DbSet<SuaEntidade>` e configure a entidade em `OnModelCreating` (chave primária,
   chaves compostas, nome de coluna para enums via `HasColumnName`, etc.), seguindo o bloco já
   existente para `Cartao`/`Cenario`.

7. **Serviço (regra de negócio)** — novo projeto `Arauco.Otimizador.Service.<Recurso>Service`
   (pasta/solution folder **Service**), com um único `.csproj` (mesmo formato do
   `Arauco.Otimizador.Service.CartaoService.csproj`: referencia `Common.Domain`, `Data.Entities`,
   `Service.Base` e `Techer.Common.Id`) e uma classe `<Recurso>Service : ServiceBase, I<Recurso>Service`.
   IDs de novas entidades são gerados com `Techer.Common.Id.IdGenerator.New()` (async, dentro de loops
   síncronos como `Select`/`GroupBy` use `IdGenerator.NewSync()`), que já produz o formato alfanumérico
   maiúsculo de 6 caracteres usado em toda a API. Erros de negócio devem usar as exceptions de
   `Techer.Common.Domain.Exceptions` (`NotFoundException` → 404, `ApiException` → 400,
   `SimultaneousAccessException` → 409) — o mapeamento para status HTTP já é feito globalmente em
   `Arauco.Otimizador.WebApi.Base/Builders/ErrorBuilder.cs`, não trate isso na controller.

8. **Controller (endpoint HTTP)** — `Controllers/<Recurso>Controller.cs`, **dentro do único projeto de
   API** `Arauco.Otimizador.WebApi` (pasta física `Arauco.Otimizador.WebApi.Cartao`, solution folder
   **WebApi**).
   - **Decisão de arquitetura**: só existe **um** projeto de API de produto além da base
     (`Arauco.Otimizador.WebApi.Base`, que é apenas biblioteca compartilhada — `BaseController`,
     `StartupBuilder`, etc., não um serviço publicável). Todo recurso novo (`Cartao`, `Cenarios`,
     `Parametros`, `Demandas`, e os que vierem depois) vira **uma nova controller dentro desse mesmo
     projeto**, e **não** um novo projeto/Lambda por recurso. Esse único projeto é publicado como uma
     única Lambda (`serverless.yml` com `Path: /{proxy+}`, sem `BasePath`), então toda controller usa
     `[Route("[controller]")]` — o nome da classe (sem o sufixo `Controller`) já vira o segmento de
     rota (`CenariosController` → `/Cenarios`, `ParametrosController` → `/Parametros`, etc.). Veja
     `Arauco.Otimizador.WebApi.Cartao/Controllers/CenariosController.cs` como referência.
     (Já existiu uma tentativa anterior de criar um projeto Lambda dedicado por recurso — foi revertida
     a pedido explícito; não repita esse padrão para novos recursos.)
   - Toda controller herda de `Arauco.Otimizador.WebApi.Base.Controller.BaseController`, que já aplica
     `[Authorize]` (JWT do Cognito) automaticamente — não é preciso reautenticar manualmente. Use
     `await GetSessionAsync()` apenas quando o endpoint realmente precisar dos dados da sessão
     (ex.: filtrar por colaborador logado, como em `CartaoController`); recursos "globais" que não são
     escopados por usuário (ex.: `CenariosController`) podem chamar o serviço diretamente.
   - No `Startup.cs` desse mesmo projeto (`Arauco.Otimizador.WebApi.Cartao/Startup.cs`), registre
     `I<Recurso>Service`/`<Recurso>Service` dentro do callback `custom` passado para
     `StartupBuilder.DefaultServicesConfiguration` (o `DbContext`/`IUnitOfWork` genérico já está
     registrado ali uma única vez para todos os recursos).
   - O `.csproj` desse projeto (`Arauco.Otimizador.WebApi.csproj`) precisa referenciar o novo projeto
     `Service.<Recurso>Service`. Registre esse `.csproj` de serviço na solução com
     `dotnet sln arauco-otimizador-api.sln add <caminho> --solution-folder Service`.
   - Exceção: os módulos legados que já existiam antes desta convenção (`Conta`/`Auth`/`DataApi`/
     `WebHook`) continuam como projetos separados e **não devem ser migrados** para o projeto único
     sem que isso seja pedido explicitamente — alguns deles dependem de infraestrutura que hoje não
     existe em lugar nenhum do repositório (gap pré-existente, não relacionado a este guia), por isso
     nem estão registrados na `.sln`.

9. **Script de banco de dados** — `Arauco.Otimizador.Deployment.Database/Scripts/ScriptNNN - <Recurso>.sql`
   Scripts DbUp numerados sequencialmente (ex.: `Script001 - Cartao.sql`, `Script002 - Otimizador.sql`),
   registrados explicitamente como `<EmbeddedResource>` no `.csproj` do projeto de deployment. São
   aplicados em ordem alfabética/numérica, uma única vez cada (DbUp controla o histórico).

### Helpers cross-cutting

Lógica reaproveitada por mais de um serviço (ex.: parser de CSV usado tanto no upload de arquivo do
cenário quanto no upload de demandas) deve morar em `Arauco.Otimizador.Common.Domain/Util/`, já que
todo projeto de serviço referencia `Common.Domain` — evita depender de outro projeto de Service só
para reusar uma função. Veja `Common.Domain/Util/DemandaCsvParser.cs`.

Helpers de infraestrutura mais "pesados" (envio de e-mail, geração de PDF, armazenamento de arquivo,
etc.) ganham seu próprio projeto `Arauco.Otimizador.Common.<Assunto>` (solution folder **Common**),
com uma classe estática de métodos utilitários — não uma interface/serviço com DI. Ex.:
`Common.Email/EmailManager.cs`, `Common.Pdf/CustomFontResolver.cs`,
`Common.Storage/LocalFileStorageHelper.cs`. Esses Helpers recebem `IEnvironmentVariables` como
parâmetro quando precisam ler configuração/ambiente, em vez de serem injetados via DI — assim podem
ser chamados diretamente de dentro de um `<Recurso>Service` sem precisar registrar mais nada no
`Startup.cs`.

**Armazenamento local de arquivos** (`Common.Storage/LocalFileStorageHelper.cs`): salva o conteúdo
recebido em disco, num diretório configurável via appsettings (`"FileStorage": { "BasePath": "..." }`);
na ausência dessa configuração, usa o diretório temporário do SO (`Path.GetTempPath()`). Isso é
proposital: em AWS Lambda o único diretório gravável é `/tmp`, que é efêmero (não sobrevive entre
invocações/cold starts) e não é compartilhado entre instâncias concorrentes da função — ou seja, esse
Helper serve para persistência de curto prazo/local (dev, ou um passo intermediário antes do
processamento), não para guarda definitiva do arquivo. Quando for necessário persistir o arquivo de
verdade em produção, troque a chamada a este Helper por `Techer.Aws.Storage.S3Helper` (o projeto já
provisiona buckets S3 — veja `## AWS Infrastructure` abaixo) sem precisar mudar a assinatura do
`<Recurso>Service` que o chama.

### Checklist rápido para um recurso novo chamado `Foo`

- `Common.Domain/Enums/Foo/...` (se houver enum fechado)
- `Common.Domain/Models/Foo/FooResponse.cs`, `FooRequest.cs`, ...
- `Common.Domain/Services/Foo/IFooService.cs`
- `Data.Entities/Foo/Foo.cs`
- `Data.Entities/IUnitOfWork.cs` + `Data.MySql/UnitOfWork.cs` (+ `DbContext.cs`)
- `Service.FooService/Service.FooService.csproj` + `FooService.cs`
- `Arauco.Otimizador.WebApi.Cartao/Controllers/FooController.cs` (o único projeto de API) +
  registro em `Startup.cs`/`.csproj` desse mesmo projeto
- `Deployment.Database/Scripts/ScriptNNN - Foo.sql`
- `dotnet sln add` para o novo `.csproj` de `Service.FooService`

## AWS Infrastructure

- **Lambda:** All APIs and functions run as Lambda functions with runtime `dotnet10`
- **API Gateway:** HTTP API routes to Lambda
- **Cognito:** User authentication and management
- **DynamoDB:** Key-value storage (`OtimizadorKeyValue`), logging (`OtimizadorLog`), workflows (`OtimizadorFlow`)
- **S3:** File storage (`arauco-otimizador-{ENV}`, `arauco-otimizador-temp-{ENV}`)
- **SQS:** Async email processing
- **Secrets Manager:** Database credentials and API keys

## Deployment

Deployment uses AWS SAM with CloudFormation. Each WebApi/Function has its own `serverless.yml`.

```bash
# Set environment
export AWS_REGION=sa-east-1
export ENVIRONMENT=dev  # dev, test, or prod
export APIDOMAIN=api.otimizador.arauco.app.br

# Deploy all APIs
./deploy_api.sh

# Deploy infrastructure
./start_deploy_setup.sh
```

**CloudFormation templates:**
- `setup.yml` - VPC, subnets, security groups, Route53
- `resources.yml` - DynamoDB tables, S3 buckets, IAM
- `resources_cognito.yml` - Cognito user pools

## Domain Enumerations

**CartaoTipoEnum:** 1=Segurança, 2=Excelência/Inovação, 3=Trabalho em Equipe, 4=Bom Cidadão, 5=Compromisso

**AplicacaoEnum:** 1=Hub

## Database

SQL migration scripts in `Database/Scripts/`:
1. `01_CreateTables.sql` - Schema creation
2. `02_InitialData.sql` - Initial seed data
3. `03_SampleData.sql` - Test data (optional)
4. `04_CreateViews.sql` - Database views
5. `05_StoredProcedures.sql` - Stored procedures

Key tables: Usuario, Cartao, Documento, AssinaturaDocumento, Perfil, Regra
