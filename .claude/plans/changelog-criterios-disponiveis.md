# Plano: Changelog 2026-08-03 — critérios tipados por enum + endpoint de listagem

Fonte: `Docs/changelog-api.md`. Duas mudanças no domínio de **critérios** (`/cenarios`). Nenhuma mudança
em `ContaController`/`DemandasController`.

## Backend

### 1. Novo enum `CriterioChaveEnum`
- **NEW** `Common.Domain/Enums/Criterio/CriterioChaveEnum.cs`: `TipoFrete = 1` com
  `[EnumMember(Value = "tipoFrete")]`. **Sem** `[JsonConverter]` no enum (padrão do projeto — aplicar no
  model).

### 2. Refatorar `Common.Domain/Util/CriteriosDisponiveis.cs`
- Passa a ser a **fonte única de verdade** do catálogo fixo (chave enum ↔ chave string ↔ nome ↔ tipo):
  `record Criterio(CriterioChaveEnum Chave, string ChaveString, string Nome, TipoCriterioEnum Tipo)`.
- Hoje: `[TipoFrete] = ("tipoFrete", "Tipo de Frete", String)`.
- Métodos: `Todos`, `ObterTipo(CriterioChaveEnum)`, `ObterNome(CriterioChaveEnum)`,
  `ObterChaveString(CriterioChaveEnum)`, `ObterChaveEnum(string)`, e `OperadorCompativel` (mantém).
- Sem reflexão, sem CRUD/tabela (spec §5.9).

### 3. Novo model `CriterioDisponivelResponse`
- **NEW** `Common.Domain/Models/Criterio/CriterioDisponivelResponse.cs`:
  `{ Chave: CriterioChaveEnum [JsonConverter(StringEnumConverter)], Nome: string, Tipo: TipoCriterioEnum [JsonConverter(StringEnumConverter)] }` (spec §3.10.1).

### 4. Tipar `criterioChave` como enum nos DTOs
- `CriterioRegraRequest`/`CriterioRegraResponse`: `CriterioChave` de `string` → `CriterioChaveEnum` com
  `[JsonConverter(typeof(StringEnumConverter))]`.
- Efeito colateral desejado: valor fora do enum → rejeitado no binding → **400 Bad Request**
  automaticamente (Newtonsoft `StringEnumConverter` + `[ApiController]`), atendendo ao item 2 do changelog.

### 5. `ICenarioService` + `CenarioService`
- **NEW** `Task<List<CriterioDisponivelResponse>> ListarCriteriosDisponiveisAsync()` → monta a resposta a
  partir de `CriteriosDisponiveis.Todos`.
- `_ValidarCriterios`: `ObterTipo` agora recebe `CriterioChaveEnum` (nulo → 400 — defesa, já que o
  binding rejeita valores fora do enum antes do service).
- `_PersistirCriterios`: enum → `ObterChaveString(enum)` ("tipoFrete") para a coluna string da entidade
  (`CenarioCriterio.CriterioChave` continua `string` — sem mudança de schema).
- `_ObterCriteriosDoCenarioAsync`: string → `ObterChaveEnum(string)`.

### 6. `CenariosController`
- **NEW** `[HttpGet("criterios-disponiveis")] ListarCriteriosDisponiveisAsync()`.
- **Precedência de rota:** em attribute routing do ASP.NET Core, segmento literal
  `criterios-disponiveis` tem precedência sobre o parâmetro `{id}` — então `/cenarios/criterios-disponiveis`
  cai no handler correto, não no `GET /cenarios/{id}`. Validar em runtime.

## Documentação de negócio

### 7. `Docs/especificacao-api.md` (atualizar para refletir o "estado final")
- **2.2** — adicionar linha `GET /cenarios/criterios-disponiveis` na tabela; nota sobre precedência de
  rota sobre `GET /cenarios/{id}` e sobre a lista ser fixa em código (servida pela API, sem CRUD).
- **3.1** — adicionar `CriterioChaveEnum` (hoje: `tipoFrete`).
- **3.9 / 3.10** — tipar `criterioChave` como `CriterioChaveEnum` (era `string`); registrar validação 400.
- **3.10.1** — adicionar schema `CriterioDisponivelResponse`.
- **7** — marcar `GET /cenarios/criterios-disponiveis` no checklist.

### 8. `CLAUDE.md`
- **Domain Enumerations**: adicionar `CriterioChaveEnum: 1=tipoFrete`.

## Não alterado
- `ContaController`, `DemandasController`, entidades (a coluna `CenarioCriterio.CriterioChave` segue
  `VARCHAR`), migration (não roda automaticamente — regra do projeto), auth (permanece removida).

## Verificação
- `dotnet build arauco-otimizador-api.sln --configuration Debug` → 0 erros.
- (Opcional/manual) rodar WebApi e chamar `GET /cenarios/criterios-disponiveis` →
  `[{ "chave":"tipoFrete", "nome":"Tipo de Frete", "tipo":"string" }]`; chamar `POST /cenarios` com
  `criterioChave` inválido → 400.