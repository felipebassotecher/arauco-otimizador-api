# Especificação da API — Arauco Otimizador de Pedidos

> Documento gerado a partir da análise dos serviços, modelos de domínio e mock APIs do front-end Angular.  
> Objetivo: servir de base para a implementação da camada de API real que substituirá as chamadas mockadas (`src/app/mock-api`).

---

## 1. Visão geral

- **Base URL padrão**: configurada em `src/environments/environment.ts` via `api.baseUrl`.
  - Dev/Test: `https://api.dev.otimizador-pedidos.arauco.app.br`
  - Prod: `https://api.otimizador-pedidos.arauco.app.br`
- **Prefixos de módulo** (definidos em `environment.api`):
  - `auth`: `/auth`
  - `conta`: `/conta`
  - `cenarios`: `/cenarios`
  - `demandas`: `/demandas`
- **Autenticação**: JWT Bearer. O `authInterceptor` injeta o token de acesso do Cognito no header `Authorization: Bearer <token>` em todas as requisições que não sejam assets.
- **Formato de data**: `Date`/`ISO 8601` (o front trata como objetos `Date` do JavaScript).
- **Idioma**: português (pt-BR) nas mensagens de erro e labels.
- **Padrão de IDs**: todos os identificadores são do tipo `string`, no formato alfanumérico uppercase de 6 caracteres, por exemplo: `ABC123`, `A1B2C3`, `X9Y8Z7`.
- **Sufixos de modelos**:
  - `*Request`: modelo enviado do front-end para o back-end.
  - `*Response`: modelo retornado do back-end para o front-end.
- **Critérios de otimização**: não existe cadastro global de critérios (não há `ParametrosController`/`CriteriosController`/`/parametros`). Os critérios disponíveis e seu tipo de dado (`TipoCriterioEnum`: `string` ou `numerico`) são definidos a nível de código. O que o usuário configura, por cenário, é uma **lista de regras** — cada uma combinando um critério + operador (`OperadorCriterioEnum`) + valor de comparação + peso (-100 a 100) —, enviada em `POST /cenarios` no campo `criterios`. O mesmo critério pode aparecer mais de uma vez na lista. Hoje existe um único critério implementado, "Tipo de Frete" (tipo `string`). Ver 2.2 e 3.4/3.9/3.10.

---

## 2. Controllers e endpoints

### 2.1 `ContaController` — prefixo `/conta`

Usado para obter o perfil do usuário autenticado.

| Método | Endpoint | Descrição | Request | Response |
|---|---|---|---|---|
| GET | `/conta/profile` | Retorna os dados do perfil do colaborador logado. | — | `PerfilResponse` |

**Response `PerfilResponse`**:

```json
{
  "colaboradorId": "ABC123",
  "nome": "João da Silva",
  "email": "joao.silva@arauco.com"
}
```

> O front consome esse endpoint em `AuthService._loadProfile()` (`src/app/core/auth/auth.service.ts`).

---

### 2.2 `CenariosController` — prefixo `/cenarios`

Controller principal do domínio de otimização.

| Método | Endpoint | Descrição | Request | Response |
|---|---|---|---|---|
| GET | `/cenarios` | Lista todos os cenários. | — | `CenarioListaResponse[]` |
| GET | `/cenarios/criterios-disponiveis` | Lista os critérios disponíveis (lista fixa em código) — usada pelo front para montar os badges de critério na tela de criação. | — | `CriterioDisponivelResponse[]` |
| GET | `/cenarios/{id}` | Retorna um cenário pelo identificador. | — | `CenarioDetalheResponse` |
| POST | `/cenarios` | Cria um novo cenário (nome e peso de cada critério — sem arquivo). | `CenarioCriacaoRequest` | `CenarioCriacaoResponse` |
| PUT | `/cenarios/{id}` | Atualiza um cenário existente. | `CenarioAtualizacaoRequest` | `CenarioDetalheResponse` |
| DELETE | `/cenarios/{id}` | Remove um cenário e seus dados relacionados (demandas/pedidos). | — | `204 No Content` |
| POST | `/cenarios/{id}/csv` | Envia o arquivo CSV de demandas do cenário. Permitido **apenas uma vez** por cenário. A API faz o parse do arquivo e cria as demandas associadas. | `multipart/form-data` — campo `arquivo` (arquivo `.csv`) | `CenarioDetalheResponse` |
| GET | `/cenarios/{id}/csv` | Baixa o arquivo CSV de demandas previamente carregado para o cenário. | — | Arquivo (`text/csv`) |
| POST | `/cenarios/{id}/processar` | Processa as demandas do cenário, gerando pedidos otimizados. | `{}` (body vazio) | `CenarioDetalheResponse` |
| GET | `/cenarios/{id}/metricas` | Retorna as métricas de processamento do cenário. | — | `CenarioMetricasResponse` |
| GET | `/cenarios/{id}/semanas/{ano}/{semana}/pedidos` | Retorna os pedidos de uma semana específica do cenário. | — | `PedidoResponse[]` |
| PATCH | `/cenarios/{id}/pedidos/mover` | Move um pedido para outra semana (fixando-o). | `MoverPedidoRequest` | `PedidoResponse` |
| POST | `/cenarios/{id}/submeter` | Submete o cenário processado ao sistema externo (SAP, futuramente). | `{}` (body vazio) | `CenarioDetalheResponse` |

> `CenarioListaResponse` (listagem) e `CenarioDetalheResponse` (detalhe/mutações) têm campos
> diferentes — a listagem não precisa de `status`, `arquivoNome`, `criterios` nem
> `primeiraSemana`/`ultimaSemana`, por exemplo. Ver 3.2/3.3.

#### Regras esperadas pela API

- `id` do cenário é do tipo `string` (ex.: `ABC123`).
- `GET /cenarios/criterios-disponiveis` retorna a **lista fixa de critérios disponíveis** (definida em
  código na API, sem CRUD/tabela — ver 3.10.1 e seção 5, item 9). O `criterioChave` usado nas regras
  (`POST`/`PUT /cenarios`) deve ser um dos valores desse enum (`CriterioChaveEnum`, ver 3.1) — valores
  fora do enum são rejeitados com `400 Bad Request`. **Precedência de rota:** a rota literal
  `criterios-disponiveis` precisa ser avaliada antes da rota com parâmetro `GET /cenarios/{id}`, para
  que a requisição não seja capturada como "cenário de id = criterios-disponiveis".
- Ao criar (`POST /cenarios`), o front envia `nome` e a lista de regras de critérios (`criterios`) — **não** envia arquivo nesta etapa. A API deve:
  1. Persistir o cenário com status `Pendente` e `arquivoNome = null`.
  2. Persistir a lista de regras (`criterioChave` + `operador` + `valor` + `peso` de cada item) junto ao próprio cenário — não é um cadastro à parte, e o mesmo `criterioChave` pode se repetir mais de uma vez na lista.
  3. Retornar apenas o identificador do cenário criado (`CenarioCriacaoResponse`).
- Ao carregar o arquivo de demandas (`POST /cenarios/{id}/csv`), disponível a partir da tela de detalhes do cenário já criado, a API deve:
  1. Rejeitar a requisição com `409 Conflict` caso o cenário já tenha um arquivo carregado (`arquivoNome` já preenchido) — **o upload só é permitido uma única vez por cenário e o arquivo não pode ser substituído depois**.
  2. Ler o arquivo enviado no campo `arquivo` do `multipart/form-data`.
  3. Criar as demandas do cenário a partir do CSV (mesmo formato descrito na seção 5 ("Pontos de atenção para a API"), item 7).
  4. Persistir `arquivoNome` com o nome do arquivo enviado e manter o conteúdo original do arquivo disponível para download.
  5. Retornar o cenário atualizado (`CenarioDetalheResponse`), já com `arquivoNome` preenchido.
- Ao baixar o arquivo de demandas (`GET /cenarios/{id}/csv`), a API deve retornar o arquivo originalmente enviado (mesmo conteúdo/nome), ou `404 Not Found` caso o cenário ainda não tenha um arquivo carregado.
- Enquanto o cenário não tiver um arquivo de demandas carregado (`arquivoNome` nulo), o front mantém a ação de processamento desabilitada — a API deve rejeitar `POST /cenarios/{id}/processar` de um cenário sem demandas (ex.: `400 Bad Request`).
- Ao processar (`POST /cenarios/{id}/processar`), a API deve:
  1. Aplicar o algoritmo de otimização considerando o peso de cada regra de `criterios` configurada no cenário (comparando o valor da demanda ao valor da regra através do operador definido).
  2. Gerar pedidos agrupados por cliente (e futuramente por outros critérios).
  3. Atualizar `status = processado`, `dataUltimoProcessamento` e retornar o cenário.
  4. Calcular `primeiraSemana` e `ultimaSemana` com base nos pedidos gerados.
- Ao submeter (`POST /cenarios/{id}/submeter`), a API deve:
  1. Alterar `submetido = true` e `status = submetido`.
  2. (Futuro) Enviar pedidos ao SAP via integração.
- Ao mover pedido (`PATCH /cenarios/{id}/pedidos/mover`), a API deve:
  1. Atualizar `ano`/`semana` do pedido.
  2. Marcar o pedido como `pinado = true`.
- Os itens de `PedidoResponse` não incluem `cenarioId` (já dado pela URL) nem `grupo` (critério de agrupamento interno do algoritmo, sem uso no front); ver 3.13.

---

### 2.3 `DemandasController` — prefixo `/demandas`

Responsável pelo gerenciamento das demandas importadas via CSV.

| Método | Endpoint | Descrição | Request | Response |
|---|---|---|---|---|
| GET | `/demandas?cenarioId={id}` | Lista as demandas de um cenário. | Query param `cenarioId` | `DemandaResponse[]` |
| POST | `/demandas/upload` | Faz upload/reimportação de demandas via CSV para um cenário. | `DemandaUploadRequest` | `DemandaResponse[]` |

#### Regras esperadas pela API

- `GET /demandas` requer o query parameter `cenarioId`.
- `POST /demandas/upload` recebe o identificador do cenário e o conteúdo textual do CSV. A API deve:
  1. Substituir as demandas existentes do cenário pelas novas.
  2. Fazer o parse das linhas no formato: `Cliente,Material,Volume,DataEntrega,TipoFrete`.
  3. Retornar a nova lista de demandas.
- Os itens de `DemandaResponse` não repetem `cenarioId` — o cliente já o informa via `cenarioId` (query param ou payload); ver 3.11.

---

## 3. Modelos de dados (schemas)

### 3.1 Enums

#### `StatusCenarioEnum`

| Valor | Descrição |
|---|---|
| `pendente` | Cenário criado, aguardando processamento. |
| `processando` | Cenário em processamento. |
| `processado` | Processamento concluído, aguardando revisão/submissão. |
| `submetido` | Cenário submetido ao sistema externo. |

#### `CriterioChaveEnum`

Chave fechada dos critérios disponíveis. `criterioChave` (em `CriterioRegraRequest`/`CriterioRegraResponse`
e `CriterioDisponivelResponse.chave`) é tipado como este enum e **transmitido como inteiro** (C# não
suporta enum de string, então a chave é um int). Valores fora do enum são rejeitados com `400 Bad Request`.
Hoje há um único critério; a lista de critérios disponíveis (com nome legível e tipo) é servida por
`GET /cenarios/criterios-disponiveis` (ver 3.10.1).

| Valor (int) | Membro | Descrição |
|---|---|---|
| `1` | `TipoFrete` | Critério "Tipo de Frete". Hoje o único critério implementado. |

#### `TipoCriterioEnum`

Tipo de dado de um critério — determina quais operadores são aplicáveis (ver `OperadorCriterioEnum`)
e como o campo `valor` de uma regra deve ser interpretado.

| Valor | Descrição |
|---|---|
| `string` | Critério cujo valor de comparação é textual (ex.: Tipo de Frete). |
| `numerico` | Critério cujo valor de comparação é numérico. |

> Este enum não é enviado/recebido via API — ele qualifica os critérios disponíveis, que são uma
> lista fixa definida em código tanto no front quanto no back (`CRITERIOS_DISPONIVEIS` no front,
> ver `src/app/domain/models/criterio/criterio.model.ts`).

#### `OperadorCriterioEnum`

Operadores de comparação disponíveis para uma regra de critério (`CriterioRegraRequest.operador`).

| Valor | Descrição | Aplicável a |
|---|---|---|
| `igual_a` | Igual a. | `string` e `numerico` |
| `diferente_de` | Diferente de. | `string` e `numerico` |
| `maior_que` | Maior que. | `numerico` |
| `menor_que` | Menor que. | `numerico` |
| `comeca_com` | Começa com. | `string` |
| `termina_com` | Termina com. | `string` |

> A API deve validar que o operador enviado é compatível com o tipo do critério referenciado por
> `criterioChave` (ex.: rejeitar `maior_que` para o critério "Tipo de Frete", que é `string`) —
> retornar `400 Bad Request` em caso de incompatibilidade.

---

### 3.2 `CenarioListaResponse`

Retornado em `GET /cenarios`. Forma resumida do cenário usada na tela de listagem — não inclui
`status`, `arquivoNome`, `criterios` nem `primeiraSemana`/`ultimaSemana`, que a listagem
não exibe.

```json
{
  "id": "ABC123",
  "nome": "string",
  "dataCriacao": "Date (ISO 8601)",
  "dataUltimoProcessamento": "Date (ISO 8601) | null",
  "submetido": "boolean"
}
```

### 3.3 `CenarioDetalheResponse`

Retornado em `GET /cenarios/{id}` e, por representarem o mesmo recurso já atualizado, também em
`PUT /cenarios/{id}`, `POST /cenarios/{id}/csv`, `POST /cenarios/{id}/processar` e
`POST /cenarios/{id}/submeter`.

```json
{
  "id": "ABC123",
  "nome": "string",
  "criterios": ["CriterioRegraResponse[]"],
  "arquivoNome": "string | null",
  "dataCriacao": "Date (ISO 8601)",
  "dataUltimoProcessamento": "Date (ISO 8601) | null",
  "status": "StatusCenarioEnum",
  "submetido": "boolean",
  "primeiraSemana": { "ano": "number", "semana": "number" } | null,
  "ultimaSemana": { "ano": "number", "semana": "number" } | null
}
```

### 3.4 `CenarioCriacaoRequest`

Enviado em `POST /cenarios`. Não contém dados do arquivo — o upload do CSV é feito em uma
requisição separada, após o cenário já existir (ver 3.4.3). `criterios` é a lista de regras
configuradas pelo usuário na tela de criação (badges de critérios → linhas de
critério/operador/valor/peso); o mesmo `criterioChave` pode se repetir mais de uma vez na lista
(ex.: uma regra para "Tipo de Frete igual a CIF" e outra para "Tipo de Frete igual a FOB").

```json
{
  "nome": "string",
  "criterios": ["CriterioRegraRequest[]"]
}
```

### 3.4.1 `CenarioCriacaoResponse`

Retornado em `POST /cenarios`. O front só usa o identificador do cenário recém-criado, para
redirecionar à tela de detalhes — por isso não retorna a representação completa do cenário.

```json
{
  "id": "ABC123"
}
```

### 3.4.2 `CenarioAtualizacaoRequest`

Enviado em `PUT /cenarios/{id}`.

```json
{
  "nome": "string",
  "criterios": ["CriterioRegraRequest[]"]
}
```

### 3.4.3 Upload do CSV de demandas (`POST /cenarios/{id}/csv`)

Diferente dos demais endpoints, este não recebe JSON: o corpo da requisição é
`multipart/form-data` com um único campo de arquivo.

| Campo | Tipo | Descrição |
|---|---|---|
| `arquivo` | `file` (`.csv`) | Arquivo de demandas no formato descrito na seção 5 ("Pontos de atenção para a API"), item 7. |

> O front (`CenarioService.enviarCsv`) monta um `FormData` com `formData.append('arquivo', arquivo, arquivo.name)`
> e faz `POST` para `/cenarios/{id}/csv` sem definir `Content-Type` manualmente (o browser define o boundary do multipart).

### 3.5 `CenarioMetricasResponse`

Retornado em `GET /cenarios/{id}/metricas`.

```json
{
  "quantidadeDemandas": "number",
  "quantidadePedidos": "number",
  "volumeTotal": "number",
  "volumePorSemana": ["CenarioMetricaSemanaResponse[]"],
  "ocupacaoPlanta": ["CenarioOcupacaoPlantaResponse[]"]
}
```

### 3.6 `CenarioMetricaSemanaResponse`

```json
{
  "ano": "number",
  "semana": "number",
  "volume": "number",
  "quantidadePedidos": "number"
}
```

### 3.7 `CenarioOcupacaoPlantaResponse`

```json
{
  "data": "Date (ISO 8601)",
  "percentual": "number (0-100)"
}
```

### 3.8 `SemanaAnoResponse`

```json
{
  "ano": "number",
  "semana": "number"
}
```

### 3.9 `CriterioRegraRequest`

Enviado como item da lista `criterios` em `CenarioCriacaoRequest`/`CenarioAtualizacaoRequest`.
Representa uma regra: um critério (`criterioChave`) comparado via `operador` a `valor`, com um
`peso` de -100 a 100 (negativo penaliza, positivo prioriza). O mesmo `criterioChave` pode
aparecer em mais de uma regra da mesma lista.

```json
{
  "criterioChave": 1,
  "operador": "OperadorCriterioEnum",
  "valor": "string",
  "peso": "number"
}
```

- `criterioChave`: valor **inteiro** de `CriterioChaveEnum` (ver 3.1) — chave de um dos critérios
  disponíveis (hoje, apenas `1` = TipoFrete). Valores fora do enum são rejeitados com `400 Bad Request`.
- `valor`: sempre transmitido como `string`; a API deve interpretá-lo como texto ou número
  conforme o `TipoCriterioEnum` do critério referenciado por `criterioChave`.
- `peso`: número inteiro entre -100 e 100.

### 3.10 `CriterioRegraResponse`

Retornado como item da lista `criterios` em `CenarioDetalheResponse. Mesma forma de
`CriterioRegraRequest` — não inclui o nome legível do critério (`criterioNome`): o front resolve
o nome a partir de `criterioChave` usando a mesma lista fixa de critérios disponíveis
(`CRITERIOS_DISPONIVEIS`) usada na tela de criação.

```json
{
  "criterioChave": 1,
  "operador": "OperadorCriterioEnum",
  "valor": "string",
  "peso": "number"
}
```

### 3.10.1 `CriterioDisponivelResponse`

Retornado em `GET /cenarios/criterios-disponiveis`. Lista fixa definida em código na API (sem
CRUD/tabela) — usada pelo front para montar os badges de critério e saber, para cada critério, seu
nome legível e seu `TipoCriterioEnum` (que determina quais operadores ficam disponíveis).

```json
[
  {
    "chave": 1,
    "nome": "Tipo de Frete",
    "tipo": "string"
  }
]
```

- `chave`: valor **inteiro** de `CriterioChaveEnum` (ver 3.1).
- `nome`: nome legível, exibido no badge e na tela de detalhes.
- `tipo`: valor de `TipoCriterioEnum` (`string` | `numerico`).

### 3.11 `DemandaResponse`

Retornado em `GET /demandas?cenarioId={id}` e `POST /demandas/upload`. Não inclui `cenarioId` —
o cliente já o informa via query param (GET) ou payload (upload).

```json
{
  "id": "X9Y8Z7",
  "cliente": "string",
  "material": "string",
  "volume": "number",
  "dataEntregaDesejada": "Date (ISO 8601)",
  "tipoFrete": "string"
}
```

> `tipoFrete` é texto livre, não um enum fechado — o "Tipo de Frete" é hoje o único critério de
> otimização implementado (ver 3.9/3.10) e seu tipo de dado é `string` (`TipoCriterioEnum.string`).
> Por convenção os valores vistos na prática são `CIF`/`FOB` (é o que o parser do CSV normaliza,
> ver item 7 da seção 5), mas a API não deve validar contra uma lista fechada de valores.

### 3.12 `DemandaUploadRequest`

Enviado em `POST /demandas/upload`.

```json
{
  "cenarioId": "ABC123",
  "conteudoCsv": "string"
}
```

### 3.13 `PedidoResponse`

Retornado em `GET /cenarios/{id}/semanas/{ano}/{semana}/pedidos` e `PATCH /cenarios/{id}/pedidos/mover`.
Não inclui `cenarioId` (já dado pela URL) nem `grupo` (critério de agrupamento interno do
algoritmo, sem uso no front).

```json
{
  "id": "X1Y2Z3",
  "cliente": "string",
  "tipoFrete": "string",
  "volume": "number",
  "dataEntregaPrevista": "Date (ISO 8601)",
  "ano": "number",
  "semana": "number",
  "pinado": "boolean"
}
```

> `tipoFrete` aqui é apenas o valor herdado das demandas agrupadas no pedido — mesma observação
> da seção 3.11 sobre não ser um enum fechado.

### 3.14 `MoverPedidoRequest`

Enviado em `PATCH /cenarios/{id}/pedidos/mover`.

```json
{
  "pedidoId": "X1Y2Z3",
  "anoDestino": "number",
  "semanaDestino": "number"
}
```

### 3.15 `PerfilResponse`

Retornado em `GET /conta/profile`.

```json
{
  "colaboradorId": "ABC123",
  "nome": "string",
  "email": "string"
}
```

---

## 4. Códigos HTTP esperados

| Código | Uso |
|---|---|
| `200 OK` | Operações de leitura/atualização bem-sucedidas. |
| `201 Created` | Criação bem-sucedida (`POST /cenarios`). |
| `204 No Content` | Exclusão bem-sucedida (`DELETE /cenarios/{id}`). |
| `400 Bad Request` | Payload inválido, CSV malformado, regra de negócio não atendida. Retornar `message` no corpo. |
| `401 Unauthorized` | Token inválido ou expirado. O front fará sign-out. |
| `403 Forbidden` | Usuário sem permissão. |
| `404 Not Found` | Recurso não encontrado (`Cenário`, `Pedido`). |
| `409 Conflict` | Conflito de estado (ex.: tentar processar cenário já submetido, ou enviar CSV de um cenário que já tem arquivo). |
| `500 Internal Server Error` | Erro inesperado. |
| `503 Service Unavailable` | Serviço temporariamente indisponível. O front redireciona para `/server-error`. |

---

## 5. Pontos de atenção para a API

1. **IDs**: todos os identificadores expostos pela API (`id`, `cenarioId`, `pedidoId`, `colaboradorId`) devem ser do tipo `string`, no formato alfanumérico uppercase de 6 caracteres (ex.: `ABC123`, `A1B2C3`, `X9Y8Z7`).
2. **Cálculo de semana ISO**: o front utiliza regra ISO 8601 para calcular ano/semana (`Utils.obterSemanaIso`). A API deve usar a mesma regra para evitar divergências na visualização semanal.
3. **Agrupamento de demandas em pedidos**: hoje o mock agrupa por cliente. Futuramente o algoritmo pode considerar outros critérios; a API deve manter o contrato `PedidoResponse`.
4. **`pinado`**: pedidos movidos manualmente devem permanecer fixos em uma nova reexecução do algoritmo.
5. **`primeiraSemana` / `ultimaSemana`**: devem ser derivados dos pedidos processados do cenário e retornados em `CenarioDetalheResponse`.
6. **`ocupacaoPlanta`**: métrica usada no gráfico de ocupação. Hoje o mock retorna dados estáticos; a API deve calcular com base na capacidade da planta e nos pedidos alocados.
7. **Formato CSV de demandas**: `Cliente,Material,Volume,DataEntrega,TipoFrete`. A coluna `TipoFrete` é interpretada case-insensitive e normalizada para `CIF` ou `FOB` (qualquer valor diferente de `CIF` vira `FOB`) — são apenas os valores usados por convenção hoje, não uma lista fechada validada pela API (`tipoFrete` é `string` livre, ver 3.11).
8. **Autenticação**: toda requisição deve passar pelo middleware de autenticação, validando o JWT Bearer enviado pelo front.
9. **Critérios não são um cadastro**: não crie um `CriteriosController`/tabela de critérios reutilizáveis entre cenários. A lista `criterios` (regras de critério + operador + valor + peso) é um dado do próprio cenário, persistido junto a ele — não uma entidade à parte, e o mesmo `criterioChave` pode se repetir na lista de um cenário. Quais critérios existem e seu `TipoCriterioEnum` são fixos em código (`CRITERIOS_DISPONIVEIS`); ao adicionar um critério novo no futuro, inclua-o nessa lista fixa (com seu tipo), sem criar uma tabela de "critérios disponíveis" no banco.
10. **Validação de operador por tipo**: ao receber uma regra em `criterios`, a API deve validar que `operador` é compatível com o `TipoCriterioEnum` do critério referenciado por `criterioChave` (ver tabela em `OperadorCriterioEnum`, seção 3.1) — rejeitar com `400 Bad Request` caso contrário.

---

## 6. Substituição futura do mock-api

No front, a transição será simples: os serviços já montam as URLs reais quando o `environment.api.baseUrl` está configurado. Hoje os serviços usam o padrão `${environment.api.baseUrl}${environment.api.<modulo>}` (ex.: `api/cenarios`), que é interceptado pelo FuseMockApi.

Quando a API real estiver disponível:

1. Os serviços já montam a URL via `${environment.api.baseUrl}${environment.api.<modulo>}` (ver `CenarioService`, `DemandaService`) — basta que `environment.api.baseUrl` aponte para a API real.
2. Remover ou desabilitar o `MockApiService` em `src/app/app.config.ts`.
3. Manter os modelos em `src/app/domain/models` e enums em `src/app/domain/enums` — eles refletem o contrato da API. Os modelos são específicos por endpoint (uma interface por request/response de cada operação, ex.: `CenarioListaResponse`, `CenarioDetalheResponse`, `CenarioCriacaoRequest`/`CenarioCriacaoResponse`) e seguem os sufixos `Request`/`Response` definidos neste documento — evite reintroduzir um único modelo genérico por entidade.

Exemplo de URL montada em `CenarioService`:

```ts
const url = `${environment.api.baseUrl}${environment.api.cenarios}`;
```

---

## 7. Checklist de controllers/endpoints a implementar

- [ ] `ContaController`
  - [ ] `GET /conta/profile`
- [ ] `CenariosController`
  - [ ] `GET /cenarios`
  - [ ] `GET /cenarios/criterios-disponiveis`
  - [ ] `GET /cenarios/{id}`
  - [ ] `POST /cenarios`
  - [ ] `PUT /cenarios/{id}`
  - [ ] `DELETE /cenarios/{id}`
  - [ ] `POST /cenarios/{id}/csv`
  - [ ] `GET /cenarios/{id}/csv`
  - [ ] `POST /cenarios/{id}/processar`
  - [ ] `GET /cenarios/{id}/metricas`
  - [ ] `GET /cenarios/{id}/semanas/{ano}/{semana}/pedidos`
  - [ ] `PATCH /cenarios/{id}/pedidos/mover`
  - [ ] `POST /cenarios/{id}/submeter`
- [ ] `DemandasController`
  - [ ] `GET /demandas?cenarioId={id}`
  - [ ] `POST /demandas/upload`
