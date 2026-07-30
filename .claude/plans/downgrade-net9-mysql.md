# Plano: Rebaixar versões para destravar a conexão MySQL (Pomelo × EF Core 10)

## Causa-raiz do `MissingMethodException`
- App roda em **net10.0** → o shared framework `Microsoft.AspNetCore.App` 10 carrega o **EF Core 10**.
- O provider **`Pomelo.EntityFrameworkCore.MySql` 9.0.0** é compilado contra o **EF Core 9** e chama o
  método interno `AbstractionsStrings.ArgumentIsEmpty(object)`, removido no EF Core 10 → crash em
  `OptionsHelper.UseMySqlLocal`.
- **Pomelo ainda não tem release para EF Core 10/.NET 10** (PR #2019 aberto, sem preview publicado;
  confirmado em nuget.org). Não dá para só "subir" o Pomelo.

## Solução adotada
Rebaixar a solution para **net9.0 + EF Core 9.0.18 + Pomelo 9.0.0**. No net9.0 o shared framework
provê o **EF Core 9.0.18** (instalado: `Microsoft.AspNetCore.App 9.0.18`), que casa com o Pomelo 9.0.0.
Verificado: o SDK .NET 10 (10.0.302, já instalado) compila projetos `net9.0`, e o runtime .NET 9
(9.0.18) está instalado para rodar.

## ⚠️ Caveat importante (decisão informada)
**.NET 9 é STS e chegou ao fim de suporte (EOL) em maio/2026.** Para **dev local** (seu objetivo atual:
banco local) funciona normalmente. Para **produção/AWS Lambda**, usar o runtime `dotnet9` (EOL) é um
risco de segurança/ausência de patches. Caminho de volta: quando o Pomelo publicar suporte a EF Core 10,
reverter este plano (voltar a net10.0/EF Core 10). Você está ciente e pediu para prosseguir.

## Alterações

### 1. TargetFramework: net10.0 → net9.0 (33 csproj)
Todos os projetos listados (`Arauco.Otimizador.*` + `Techer.*`). Necessário porque um app net9.0 não
pode consumir libs net10.0. Isso toca também as libs `Techer.*` (marcadas "kept unchanged" no
CLAUDE.md) — inevitável para a solution compilar como um todo.

### 2. Versões de pacote 10.0.x → 9.0.18 (ou 9.x)
| Pacote | De | Para | Onde |
|---|---|---|---|
| `Microsoft.EntityFrameworkCore` | 10.0.10 | **9.0.18** | Service.Base, Techer.Common.Extensions, Techer.Data.MySql |
| `Pomelo.EntityFrameworkCore.MySql` | 9.0.0 | 9.0.0 (mantém) | Techer.Data.MySql |
| `Microsoft.AspNetCore.OpenApi` | 10.0.10 | **9.0.18** | WebApi |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.10 | **9.0.18** | Techer.Common.WebApi |
| `Microsoft.AspNetCore.Mvc.NewtonsoftJson` | 10.0.10 | **9.0.18** | Techer.Common.WebApi |
| `Amazon.Lambda.AspNetCoreServer` | 10.1.1 | **9.2.1** | Techer.Common.WebApi (10.x não declara net9.0) |
| `Microsoft.Extensions.Configuration.Abstractions` | 10.0.10 | **9.0.18** | Data.MySql |
| `Microsoft.Extensions.Configuration.Json` | 10.0.10 | **9.0.18** | Deployment.Database |

9.0.18 escolhido para casar exatamente com o shared framework 9.0.18 instalado (evita warnings NU1605).

### 3. Runtime/deploy (Lambda + CI)
- `deploy_api.sh`: `--framework net10.0` → `net9.0`
- `buildspec.yml`: `dotnet: 10.0` → `9.0`
- `Arauco.Otimizador.Function.EmailOutbound/serverless.yml`: `Runtime: dotnet10` → `dotnet9`

### 4. CLAUDE.md
- Linha 7: ".NET 10.0" → ".NET 9.0"
- Linha 28: `--framework net10.0` → `net9.0`
- Linha 239: "runtime `dotnet10`" → "runtime `dotnet9`"

### 5. global.json — **sem mudança**
SDK 10.0.301 (rollForward latestFeature → 10.0.302) compila net9.0 (verificado). Mantém.

## O que NÃO muda
- Código-fonte (controllers, services, DbContext, UnitOfWork, OptionsHelper, appsettings) — só
  versões de framework/pacotes.
- Outros pacotes (AWSSDK.*, Amazon.Lambda.Core/Serialization/SQSEvents, Newtonsoft, Polly, PDFsharp,
  Ulid, Nanoid, FluentValidation, dbup-mysql, libphonenumber, Mustache, System.Linq.Dynamic.Core) —
  versões independentes de TFM, permanecem.

## Riscos / verificação
- **C# 14 vs 13**: se algum código usar sintaxe só do C# 14 (ex.: keyword `field`, extension members),
  o build em net9.0 (C# 13) falha. O build vai pegar; se ocorrer, ajusto caso a caso.
- `dotnet restore` + `dotnet build arauco-otimizador-api.sln --configuration Debug` → 0 erros.
- Rodar WebApi local + `GET /check` (200) e um endpoint que toque o banco (conexão MySQL local via
  `ConnectionStrings:DefaultConnection`, sem `MissingMethodException`).
- `dotnet run --project Arauco.Otimizador.Deployment.Database` aplica o schema no MySQL local.

## Pós-aplicação
Limpar `bin/`/`obj/` antes do build (mudança de TFM gera lixo de net10.0).