# Plano: Remover a camada de autenticação/autorização da WebApi

## Contexto — por que `/check` retorna 401

A cadeia de auth herda do projeto de exemplo (Cognito + JWT Bearer):

1. `BaseController` (WebApi.Base) está decorado com **`[Authorize]**. Todas as controllers
   (`DefaultController`, `CartaoController`, `CenariosController`, `DemandasController`,
   `ParametrosController`) herdam dela → todo endpoint exige usuário autenticado.
2. `StartupBuilder.DefaultServicesConfiguration` registra `AddAuthentication` (JWT Bearer) com
   `Authority`/`Audience` do Cognito e define `DefaultPolicy = RequireAuthenticatedUser()`.
3. `StartupBuilder.DefaultConfiguration` chama `app.UseAuthentication()` + `app.UseAuthorization()`.
4. `Startup.cs` passa `Cognitos.App` (`AwsResource<CognitoData>`) para o builder.

Resultado: sem token JWT válido do Cognito → 401 Unauthorized em `/check`.

## Escopo

Remover **apenas a camada de autenticação/autorização** (o mecanismo que bloqueia requisições).
Manter o restante do pipeline (CORS, exception handler, routing, endpoints, serialização Newtonsoft,
registro de DbContext/UnitOfWork/serviços).

### Arquivos alterados (4)

#### 1. `Arauco.Otimizador.WebApi.Base/Controller/BaseController.cs`
- Remover o atributo `[Authorize]` da classe.
- Remover `using Microsoft.AspNetCore.Authorization;`.
- Manter `[ApiController]`, o construtor e `GetSessionAsync()` (que já retorna `null` —
  `CartaoController` chama `await GetSessionAsync()` e o `CartaoService` recebe `null`; não quebra).

#### 2. `Arauco.Otimizador.WebApi.Base/Builders/StartupBuilder.cs`
- Remover todo o bloco de auth: `AddAuthentication`/`AddJwtBearer`/`AddAuthorization` + o
  `foreach (var pool in pools)` e a variável `environmentEnum`/`authSchemes` (linhas ~42-75).
- Substituir as **duas** sobrecargas de `DefaultServicesConfiguration(...)` por **uma única**:
  ```csharp
  public static void DefaultServicesConfiguration(
      IConfiguration config, IWebHostEnvironment env,
      IServiceCollection services, Action<IServiceCollection> custom)
  ```
  (sem o parâmetro `AwsResource<CognitoData> pool` / `List<...> pools`).
- Em `DefaultConfiguration`: remover `app.UseAuthentication();` e `app.UseAuthorization();`.
  Manter `UseCors`, `UseExceptionHandler`, `UseHttpsRedirection`, `UseRouting`, `UseEndpoints`.
- Remover usings que ficam sem uso:
  - `Microsoft.AspNetCore.Authentication.JwtBearer`
  - `Microsoft.AspNetCore.Authorization`
  - `Techer.Aws.Cognito.Models` (`CognitoData`)
  - `Techer.Aws.Shared` (`AwsResource<>`)
- Manter os registros de `IEnvironmentVariables`, `ISessionManager<AppSessionModel>`,
  `IUserIdentity`, `IKeyValueRepository`, `ILogRepository` e o `custom(services)`.

#### 3. `Arauco.Otimizador.WebApi/Startup.cs`
- Trocar a chamada
  `DefaultServicesConfiguration(Configuration, Env, services, Cognitos.App, s => { ... })`
  por `DefaultServicesConfiguration(Configuration, Env, services, s => { ... })`.
- Remover `using Arauco.Otimizador.Aws.Shared;` (era só para `Cognitos.App`).

#### 4. Sem alteração de controller — nenhuma controller usa `[Authorize]`/`[AllowAnonymous]`
   próprio (confirmado por grep). Herdavam o `[Authorize]` só via `BaseController`.

### O que NÃO será removido (mantido, inofensivo)
- `AppUserIdentity`, `IUserIdentity`, `ISessionManager<AppSessionModel>`, `AppSessionManager`,
  `InvalidSessionException`, o ramo `InvalidSessionException → 401` no `ErrorBuilder`.
  São infraestrutura de **sessão** (genérica, em `Common.Domain`), já efetivamente morta
  (`BaseController` não injeta mais `sessionManager`/`tokenData`; `GetSessionAsync()` retorna
  `null`). Sem `[Authorize]` e sem middleware de auth, elas nunca causam 401. Removê-las exigiria
  tocar interfaces compartilhadas e o `CartaoService` (que recebe `AppSessionModel`) — fora do
  escopo e desnecessário para o objetivo. Posso limpar depois, se quiser.

## Verificação
- `dotnet build arauco-otimizador-api.sln --configuration Debug` → 0 erros.
- Rodar `Arauco.Otimizador.WebApi` localmente e `GET /check` → `200 OK` sem auth.

## Achado extra (não faz parte desta tarefa)
`Arauco.Otimizador.WebApi/Arauco.Otimizador.WebApi.csproj` está cheio de entradas de build
artifacts (`<Compile Include="obj\Debug\...">`, `<Content Include="bin\Debug\...">`,
dezenas de `<None Include="bin\Debug\net10.0\*.dll">`). Isso é lixo que foi parar no .csproj
por engano (operação errada no VS) e pode causar comportamento estranho no build local.
Não vou mexer a menos que você peça — mas vale uma limpeza separada.