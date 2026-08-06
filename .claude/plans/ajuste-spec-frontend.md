# Plano: Ajustar a API ao contrato do front-end (`Docs/especificacao-api.md`)

## Decisão já tomada com você
- **Auth:** permanece **removida** (sem JWT Bearer). O ponto 8 da spec NÃO será atendido. `GET /conta/profile`
  será um **stub** (perfil fixo), pois sem usuário autenticado não há dados reais.

## Princípios
- Manter a arquitetura em camadas (Controller → Service → UnitOfWork → DbContext) e o padrão de IDs
  (`IdGenerator`, alfanumérico uppercase 6 chars), já compatível com a spec.
- `tipoFrete`: permanece **enum (`TipoFreteEnum`)** internamente (entidade + parser já normalizam
  CIF/FOB, o que casa com a regra de normalização da spec item 7). Nos **DTOs** exposto como **string**
  ("CIF"/"FOB") — sem validação de lista fechada (spec §3.11/3.13). **Sem alteração de schema** nas
  tabelas Demanda/Pedido.
- Rotas `[Route("[controller]")]` (PascalCase) — ASP.NET Core faz matching **case-insensitive**, então
  `/cenarios`/`/demandas`/`/conta` batem. Mantido o padrão do projeto.

## Escopo: 14 frentes de trabalho

### F1 — Enums (`Common.Domain/Enums/Criterio/`)
- **NEW** `OperadorCriterioEnum`: `Igual_a=1, Diferente_de=2, Maior_que=3, Menor_que=4, Comeca_com=5,
  Termina_com=6` — cada um com `[EnumMember(Value="igual_a")]` etc. **Sem** `[JsonConverter]` no enum
  (aplicar na propriedade do model, por guia do CLAUDE.md).
- **NEW** `TipoCriterioEnum`: `String=1, Numerico=2` (idem, `[EnumMember]` `string`/`numerico`).
- `StatusCenarioEnum` já bate com a spec — mantido.

### F2 — Critérios: catálogo fixo em código + models (`Common.Domain/Models/Criterio/`)
- **NEW** `CriterioRegraRequest { CriterioChave: string, Operador: OperadorCriterioEnum [JsonConverter(StringEnumConverter)], Valor: string, Peso: int }`.
- **NEW** `CriterioRegraResponse { CriterioChave, Operador: OperadorCriterioEnum [JsonConverter], Valor, Peso }`.
- **NEW** `Common.Domain/Util/CriteriosDisponiveis.cs` — `static` com o mapa `chave → TipoCriterioEnum`
  (hoje só `"tipoFrete" → String`). Usado na validação operador×tipo (spec item 10).

### F3 — Models Cenario (`Models/Cenario/`)
- `CenarioListaResponse` → **slim**: `{ Id, Nome, DataCriacao, DataUltimoProcessamento, Submetido }`.
  Remover `ArquivoNome, Status, PrimeiraSemana, UltimaSemana`.
- `CenarioDetalheResponse` → trocar `Parametros: List<ParametroListaResponse>` por
  `Criterios: List<CriterioRegraResponse>`. Manter o resto.
- `CenarioCriacaoRequest` → trocar `ParametroIds: List<string>` por `Criterios: List<CriterioRegraRequest>`.
- `CenarioCriacaoResponse` → **slim** para `{ Id }`.
- **NEW** `CenarioAtualizacaoRequest { Nome, Criterios: List<CriterioRegraRequest> }`.
- **REMOVER** `CenarioUploadArquivoResponse`, `CenarioProcessamentoResponse`, `CenarioSubmissaoResponse`
  (substituídos por `CenarioDetalheResponse`).
- Mantidos: `CenarioMetricasResponse`, `CenarioMetricaSemanaResponse`, `CenarioOcupacaoPlantaResponse`,
  `SemanaAnoResponse` (já batem com a spec).

### F4 — Models Demanda (`Models/Demanda/`)
- **NEW** `DemandaResponse { Id, Cliente, Material, Volume: decimal, DataEntregaDesejada: DateTime,
  TipoFrete: string }` (sem `CenarioId`; `tipoFrete` string).
- Mantido `DemandaUploadRequest { CenarioId, ConteudoCsv }`.
- **REMOVER** `DemandaListaResponse`, `DemandaUploadResponse`.

### F5 — Models Pedido (`Models/Pedido/`)
- **NEW** `PedidoResponse { Id, Cliente, TipoFrete: string, Volume, DataEntregaPrevista, Ano, Semana,
  Pinado }` (sem `CenarioId`, sem `Grupo`; `tipoFrete` string).
- **NEW** `MoverPedidoRequest { PedidoId, AnoDestino, SemanaDestino }` (renomeia `PedidoMovimentacaoRequest`).
- **REMOVER** `PedidoListaResponse`, `PedidoMovimentacaoRequest`, `PedidoMovimentacaoResponse`.

### F6 — Models Conta (`Models/Conta/`)
- **NEW** `PerfilResponse { ColaboradorId: string, Nome: string, Email: string }`.

### F7 — Entidades (`Data.Entities/`)
- **NEW** `Cenario/CenarioCriterio.cs { Id: int (auto), CenarioId: string, CriterioChave: string,
  Operador: OperadorCriterioEnum, Valor: string, Peso: int }` — regras de critério **pertencentes ao
  cenário** (mesmo `criterioChave` pode repetir). PK = `Id` identity.
- **NEW** `Cenario/CenarioArquivo.cs { CenarioId: string (PK), Nome: string, Conteudo: string (LONGTEXT),
  DataUpload: DateTime }` — conteúdo original do CSV para download (carregado só no endpoint de download).
- **REMOVER** `Cenario/CenarioParametro.cs`, `Parametro/Parametro.cs`, `Parametro/ParametroValor.cs`.
- `Cenario`, `Demanda`, `Pedido` — mantidos (tipoFrete enum interno).

### F8 — DbContext + UnitOfWork
- `DbContext.cs`: remover DbSets/config de `CenarioParametro`, `Parametro`, `ParametroValor`;
  adicionar `DbSet<CenarioCriterio>` (config: PK `Id`, `Operador`→coluna `OperadorId`) e
  `DbSet<CenarioArquivo>` (PK `CenarioId`).
- `IUnitOfWork` + `UnitOfWork`: remover `CenarioParametroRepository`, `ParametroRepository`,
  `ParametroValorRepository`; adicionar `CenarioCriterioRepository`, `CenarioArquivoRepository`.

### F9 — Migration (`Deployment.Database/Scripts/`)
- Editar `Script001 - Otimizador.sql`: remover `CREATE TABLE` de `Parametro`, `ParametroValor`,
  `CenarioParametro`; adicionar `CREATE TABLE` de `CenarioCriterio` e `CenarioArquivo`.
- **NEW** `Script003 - Criterios.sql`: `DROP TABLE IF EXISTS` das 3 tabelas antigas;
  `CREATE TABLE IF NOT EXISTS CenarioCriterio/​CenarioArquivo` (cobre DBs que já rodaram o Script001
  antigo — DbUp não re-roda script já aplicado).
- Atualizar `EmbeddedResource` no `.csproj` do Deployment.Database.

### F10 — CenarioService (rewrite) + ICenarioService
- `ListarAsync()` → `List<CenarioListaResponse>` (slim).
- `ObterAsync(id)` → `CenarioDetalheResponse` com `Criterios` mapeados de `CenarioCriterio`.
- `CriarAsync(CenarioCriacaoRequest)` → `CenarioCriacaoResponse { Id }`: valida critérios (chave
  existe; operador compatível com `TipoCriterioEnum` da chave → 400; `peso` -100..100 → 400);
  cria Cenario (`status=Pendente`, `arquivoNome=null`) + linhas `CenarioCriterio`.
- **NEW** `AtualizarAsync(id, CenarioAtualizacaoRequest)` → `CenarioDetalheResponse`: atualiza `nome`,
  substitui regras (apaga antigas, insere novas) com validação.
- `UploadArquivoAsync(id, IFormFile)` → `CenarioDetalheResponse`: **409 se `arquivoNome` já preenchido**
  (`SimultaneousAccessException`→409); parse CSV→demandas; persistir `arquivoNome` + `CenarioArquivo`
  (nome+conteúdo); retornar detalhe.
- **NEW** `DownloadArquivoAsync(id)` → `(nome, conteudo)` ou `NotFoundException` (404) se sem arquivo.
- `ProcessarAsync(id)` → `CenarioDetalheResponse`: **400 se `arquivoNome` nulo** (`ApiException`); rodar
  algoritmo (ver nota abaixo); `status=Processado`, `dataUltimoProcessamento`, `primeiraSemana`/`ultimaSemana`.
- `ObterMetricasAsync(id)` → mantido (refinar `ocupacaoPlanta` se simples).
- `ListarPedidosDaSemanaAsync(id, ano, semana)` → `List<PedidoResponse>` (sem `cenarioId`/`grupo`).
- `MoverPedidoAsync(id, MoverPedidoRequest)` → `PedidoResponse` (`ano`/`semana` atualizados, `pinado=true`).
- `SubmeterAsync(id)` → `CenarioDetalheResponse` (`submetido=true`, `status=Submetido`).
- `RemoverAsync(id)` → também remove `CenarioCriterio` e `CenarioArquivo`.
- **Algoritmo (MVP por spec §2.2/§5.3):** manter agrupamento por **cliente + semana ISO** (comportamento
  atual e igual ao mock, spec §5.3 "hoje agrupa por cliente"); os `criterios` ficam **persistidos** mas a
  aplicação dos **pesos** na otimização é "futuramente" (spec §5.3). Contrato `PedidoResponse` mantido.

### F11 — DemandaService + IDemandaService
- `ListarAsync(cenarioId)` → `List<DemandaResponse>` (sem `cenarioId`, `tipoFrete` string).
- `UploadAsync(DemandaUploadRequest)` → `List<DemandaResponse>`: **substitui** demandas existentes pelas
  novas (spec §2.3), parse via `DemandaCsvParser`. (Não mexe em `arquivoNome`/arquivo — esse é só do
  `/cenarios/{id}/csv`.)

### F12 — ContaService + IContaService (novo, stub) + ContaController
- `Common.Domain/Services/Conta/IContaService.cs`: `Task<PerfilResponse> ObterPerfilAsync();`.
- **NEW** projeto `Arauco.Otimizador.Service.ContaService` (`ContaService : ServiceBase, IContaService`):
  `ObterPerfilAsync()` retorna **stub** `PerfilResponse` (perfil fixo) — sem auth, sem colaborador real.
- **NEW** `WebApi/Controllers/ContaController.cs`: `[Route("[controller]")]`, `GET "profile"` →
  `PerfilResponse` (200).

### F13 — Remoção do módulo Parametro (contrário à spec §1/§3.9/§5.9)
- **REMOVER** `WebApi/Controllers/ParametrosController.cs`.
- **REMOVER** `Common.Domain/Services/Parametro/IParametroService.cs` + projeto
  `Arauco.Otimizador.Service.ParametroService` (e do `.sln`).
- **REMOVER** `Common.Domain/Models/Parametro/*` (8 arquivos).
- `Startup.cs`: remover registro de `IParametroService`; adicionar `IContaService`.
- `WebApi.csproj`: remover ref a `Service.ParametroService`; adicionar ref a `Service.ContaService`.
- `dotnet sln add .../Service.ContaService --solution-folder Service` e `dotnet sln remove .../Service.ParametroService`.

### F14 — CenariosController / DemandasController (ajustes de endpoints)
- `CenariosController`: ajustar retornos — `POST ""` → **201 Created** (`CreatedAtAction`) +
  `CenarioCriacaoResponse`; `POST "{id}/csv"` → `CenarioDetalheResponse`; `GET "{id}/csv"` →
  `FileResult` (text/csv, nome original) [NEW]; `POST "{id}/processar"` → `CenarioDetalheResponse`;
  `PATCH "{id}/pedidos/mover"` → `PedidoResponse` (`MoverPedidoRequest`); `POST "{id}/submeter"` →
  `CenarioDetalheResponse`; `GET "{id}/semanas/{ano}/{semana}/pedidos"` → `List<PedidoResponse>`;
  `PUT "{id}"` → `CenarioDetalheResponse` [NEW]; `GET ""`/`GET "{id}"`/`DELETE` ajustados aos novos DTOs.
- `DemandasController`: `GET ""` → `List<DemandaResponse>`; `POST "upload"` → `List<DemandaResponse>`.

## Não alterado / fora de escopo
- `Cartao` (controller/service/entities) — **mantido** (exemplo canônico, CLAUDE.md). **Atenção:** está
  quebrado em runtime (`GetSessionAsync()` retorna `null` → `CartaoService.ListarAsync(null)` NPE),
  mas não faz parte da spec — não mexo. Avise se quiser limpar o session morto do Cartão.
- Auth/JWT — permanece removida (decisão sua).
- Tabelas Demanda/Pedido (schema `TipoFreteId INT`) — sem mudança.

## Verificação
1. `dotnet build arauco-otimizador-api.sln --configuration Debug` → 0 erros.
2. Rodar `dotnet run --project Arauco.Otimizador.Deployment.Database` para aplicar `Script003` no MySQL
   local (cria `CenarioCriterio`/`CenarioArquivo`, remove tabelas antigas).
3. Rodar WebApi e exercitar: `GET /cenarios`, `POST /cenarios` (201 + só `{id}`), `PUT /cenarios/{id}`,
   `POST /cenarios/{id}/csv` (upload), `GET /cenarios/{id}/csv` (download), `POST /{id}/processar` (400
   sem arquivo; 200 com), `GET /{id}/metricas`, `GET /{id}/semanas/{ano}/{semana}/pedidos`,
   `PATCH /{id}/pedidos/mover`, `POST /{id}/submeter`, `DELETE /{id}`; `GET /demandas?cenarioId=`,
   `POST /demandas/upload`; `GET /conta/profile` (stub). Conferir `criterios` persistidos e a rejeição
   de operador incompatível (400) e re-upload de CSV (409).