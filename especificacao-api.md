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
  - `parametros`: `/parametros`
  - `demandas`: `/demandas`
- **Autenticação**: JWT Bearer. O `authInterceptor` injeta o token de acesso do Cognito no header `Authorization: Bearer <token>` em todas as requisições que não sejam assets.
- **Formato de data**: `Date`/`ISO 8601` (o front trata como objetos `Date` do JavaScript).
- **Idioma**: português (pt-BR) nas mensagens de erro e labels.
- **Padrão de IDs**: todos os identificadores são do tipo `string`, no formato alfanumérico uppercase de 6 caracteres, por exemplo: `ABC123`, `A1B2C3`, `X9Y8Z7`.
- **Sufixos de modelos**:
  - `*Request`: modelo enviado do front-end para o back-end.
  - `*Response`: modelo retornado do back-end para o front-end.

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
| GET | `/cenarios` | Lista todos os cenários. | — | `CenarioResponse[]` |
| GET | `/cenarios/{id}` | Retorna um cenário pelo identificador. | — | `CenarioResponse` |
| POST | `/cenarios` | Cria um novo cenário. | `CenarioCriarRequest` | `CenarioResponse` |
| PUT | `/cenarios/{id}` | Atualiza um cenário existente. | `CenarioRequest` | `CenarioResponse` |
| DELETE | `/cenarios/{id}` | Remove um cenário e seus dados relacionados (demandas/pedidos). | — | `204 No Content` |
| POST | `/cenarios/{id}/processar` | Processa as demandas do cenário, gerando pedidos otimizados. | `{}` (body vazio) | `CenarioResponse` |
| GET | `/cenarios/{id}/metricas` | Retorna as métricas de processamento do cenário. | — | `CenarioMetricasResponse` |
| GET | `/cenarios/{id}/semanas/{ano}/{semana}/pedidos` | Retorna os pedidos de uma semana específica do cenário. | — | `PedidoResponse[]` |
| PATCH | `/cenarios/{id}/pedidos/mover` | Move um pedido para outra semana (fixando-o). | `MoverPedidoRequest` | `PedidoResponse` |
| POST | `/cenarios/{id}/submeter` | Submete o cenário processado ao sistema externo (SAP, futuramente). | `{}` (body vazio) | `CenarioResponse` |

#### Regras esperadas pela API

- `id` do cenário é do tipo `string` (ex.: `ABC123`).
- Ao criar (`POST /cenarios`), o front envia o conteúdo textual do CSV junto com o payload. A API deve:
  1. Persistir o cenário com status `Pendente`.
  2. Parsear `conteudoCsv` e criar as demandas associadas.
  3. Retornar o cenário criado com `status = pendente` e `submetido = false`.
- Ao processar (`POST /cenarios/{id}/processar`), a API deve:
  1. Aplicar o algoritmo de otimização considerando os parâmetros selecionados.
  2. Gerar pedidos agrupados por cliente (e futuramente por outros parâmetros).
  3. Atualizar `status = processado`, `dataUltimoProcessamento` e retornar o cenário.
  4. Calcular `primeiraSemana` e `ultimaSemana` com base nos pedidos gerados.
- Ao submeter (`POST /cenarios/{id}/submeter`), a API deve:
  1. Alterar `submetido = true` e `status = submetido`.
  2. (Futuro) Enviar pedidos ao SAP via integração.
- Ao mover pedido (`PATCH /cenarios/{id}/pedidos/mover`), a API deve:
  1. Atualizar `ano`/`semana` do pedido.
  2. Marcar o pedido como `pinado = true`.

---

### 2.3 `ParametrosController` — prefixo `/parametros`

Gerencia os parâmetros de otimização usados na criação de cenários.

| Método | Endpoint | Descrição | Request | Response |
|---|---|---|---|---|
| GET | `/parametros` | Lista todos os parâmetros. | — | `ParametroResponse[]` |
| GET | `/parametros/ativos` | Lista apenas os parâmetros ativos (selecionáveis em novos cenários). | — | `ParametroResponse[]` |
| GET | `/parametros/{id}` | Retorna um parâmetro pelo identificador. | — | `ParametroResponse` |
| POST | `/parametros` | Cria um novo parâmetro. | `ParametroRequest` | `ParametroResponse` |
| PUT | `/parametros/{id}` | Atualiza um parâmetro existente. | `ParametroRequest` | `ParametroResponse` |
| DELETE | `/parametros/{id}` | Remove um parâmetro. | — | `204 No Content` |

#### Regras esperadas pela API

- `id` do parâmetro é do tipo `string` (ex.: `A1B2C3`).
- A chave `chave` deve ser única (ex.: `tipoFrete`, `prazoEntrega`).
- O campo `valores` é opcional; quando presente, representa valores possíveis com rótulo legível e peso.
- Apenas parâmetros com `ativo = true` devem ser retornados em `/parametros/ativos`.

---

### 2.4 `DemandasController` — prefixo `/demandas`

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

#### `TipoFreteEnum`

| Valor | Descrição |
|---|---|
| `CIF` | Frete por conta do vendedor. |
| `FOB` | Frete por conta do comprador. |

---

### 3.2 `CenarioResponse`

Retornado em `GET /cenarios`, `GET /cenarios/{id}`, `POST /cenarios`, `PUT /cenarios/{id}`, `POST /cenarios/{id}/processar` e `POST /cenarios/{id}/submeter`.

```json
{
  "id": "ABC123",
  "nome": "string",
  "parametros": ["ParametroResponse[]"],
  "arquivoNome": "string",
  "dataCriacao": "Date (ISO 8601)",
  "dataUltimoProcessamento": "Date (ISO 8601) | null",
  "status": "StatusCenarioEnum",
  "submetido": "boolean",
  "primeiraSemana": { "ano": "number", "semana": "number" } | null,
  "ultimaSemana": { "ano": "number", "semana": "number" } | null
}
```

### 3.3 `CenarioRequest`

Enviado em `PUT /cenarios/{id}`.

```json
{
  "id": "ABC123",
  "nome": "string",
  "parametros": ["ParametroRequest[]"],
  "arquivoNome": "string",
  "dataCriacao": "Date (ISO 8601)",
  "dataUltimoProcessamento": "Date (ISO 8601) | null",
  "status": "StatusCenarioEnum",
  "submetido": "boolean",
  "primeiraSemana": { "ano": "number", "semana": "number" } | null,
  "ultimaSemana": { "ano": "number", "semana": "number" } | null
}
```

### 3.4 `CenarioCriarRequest`

Enviado em `POST /cenarios`.

```json
{
  "nome": "string",
  "parametroIds": ["string[]"],
  "arquivoNome": "string",
  "conteudoCsv": "string"
}
```

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

### 3.9 `ParametroResponse`

Retornado em `GET /parametros`, `GET /parametros/ativos`, `GET /parametros/{id}`, `POST /parametros` e `PUT /parametros/{id}`. Também usado como objeto aninhado em `CenarioResponse`.

```json
{
  "id": "A1B2C3",
  "nome": "string",
  "chave": "string",
  "descricao": "string",
  "peso": "number",
  "ativo": "boolean",
  "valores": ["ParametroValorResponse[]"] | null
}
```

### 3.10 `ParametroRequest`

Enviado em `POST /parametros` e `PUT /parametros/{id}`.

```json
{
  "id": "A1B2C3",
  "nome": "string",
  "chave": "string",
  "descricao": "string",
  "peso": "number",
  "ativo": "boolean",
  "valores": ["ParametroValorRequest[]"] | null
}
```

### 3.11 `ParametroValorResponse`

```json
{
  "valor": "string",
  "rotulo": "string",
  "peso": "number | null"
}
```

### 3.12 `ParametroValorRequest`

```json
{
  "valor": "string",
  "rotulo": "string",
  "peso": "number | null"
}
```

### 3.13 `DemandaResponse`

Retornado em `GET /demandas?cenarioId={id}` e `POST /demandas/upload`.

```json
{
  "id": "X9Y8Z7",
  "cenarioId": "ABC123",
  "cliente": "string",
  "material": "string",
  "volume": "number",
  "dataEntregaDesejada": "Date (ISO 8601)",
  "tipoFrete": "TipoFreteEnum"
}
```

### 3.14 `DemandaUploadRequest`

Enviado em `POST /demandas/upload`.

```json
{
  "cenarioId": "ABC123",
  "conteudoCsv": "string"
}
```

### 3.15 `PedidoResponse`

Retornado em `GET /cenarios/{id}/semanas/{ano}/{semana}/pedidos` e `PATCH /cenarios/{id}/pedidos/mover`.

```json
{
  "id": "X1Y2Z3",
  "cenarioId": "ABC123",
  "cliente": "string",
  "tipoFrete": "TipoFreteEnum",
  "volume": "number",
  "dataEntregaPrevista": "Date (ISO 8601)",
  "ano": "number",
  "semana": "number",
  "pinado": "boolean",
  "grupo": "string | null"
}
```

### 3.16 `MoverPedidoRequest`

Enviado em `PATCH /cenarios/{id}/pedidos/mover`.

```json
{
  "pedidoId": "X1Y2Z3",
  "anoDestino": "number",
  "semanaDestino": "number"
}
```

### 3.17 `PerfilResponse`

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
| `201 Created` | Criação bem-sucedida (`POST /cenarios`, `POST /parametros`). |
| `204 No Content` | Exclusão bem-sucedida (`DELETE /cenarios/{id}`, `DELETE /parametros/{id}`). |
| `400 Bad Request` | Payload inválido, CSV malformado, regra de negócio não atendida. Retornar `message` no corpo. |
| `401 Unauthorized` | Token inválido ou expirado. O front fará sign-out. |
| `403 Forbidden` | Usuário sem permissão. |
| `404 Not Found` | Recurso não encontrado (`Cenário`, `Parâmetro`, `Pedido`). |
| `409 Conflict` | Conflito de estado (ex.: tentar processar cenário já submetido). |
| `500 Internal Server Error` | Erro inesperado. |
| `503 Service Unavailable` | Serviço temporariamente indisponível. O front redireciona para `/server-error`. |

---

## 5. Pontos de atenção para a API

1. **IDs**: todos os identificadores expostos pela API (`id`, `cenarioId`, `pedidoId`, `parametroIds`, `colaboradorId`) devem ser do tipo `string`, no formato alfanumérico uppercase de 6 caracteres (ex.: `ABC123`, `A1B2C3`, `X9Y8Z7`).
2. **Cálculo de semana ISO**: o front utiliza regra ISO 8601 para calcular ano/semana (`Utils.obterSemanaIso`). A API deve usar a mesma regra para evitar divergências na visualização semanal.
3. **Agrupamento de demandas em pedidos**: hoje o mock agrupa por cliente. Futuramente o algoritmo pode considerar outros parâmetros; a API deve manter o contrato `PedidoResponse`.
4. **`pinado`**: pedidos movidos manualmente devem permanecer fixos em uma nova reexecução do algoritmo.
5. **`primeiraSemana` / `ultimaSemana`**: devem ser derivados dos pedidos processados do cenário e retornados no `CenarioResponse`.
6. **`ocupacaoPlanta`**: métrica usada no gráfico de ocupação. Hoje o mock retorna dados estáticos; a API deve calcular com base na capacidade da planta e nos pedidos alocados.
7. **Formato CSV de demandas**: `Cliente,Material,Volume,DataEntrega,TipoFrete`. O tipo de frete é case-insensitive (`CIF`/`FOB`), defaultando para `FOB`.
8. **Autenticação**: toda requisição deve passar pelo middleware de autenticação, validando o JWT Bearer enviado pelo front.

---

## 6. Substituição futura do mock-api

No front, a transição será simples: os serviços já montam as URLs reais quando o `environment.api.baseUrl` está configurado. Hoje os serviços usam o padrão `api${environment.api.<modulo>}` (ex.: `api/cenarios`), que é interceptado pelo FuseMockApi.

Quando a API real estiver disponível:

1. Alterar os serviços para usar `${environment.api.baseUrl}${environment.api.<modulo>}` (já existe em `AuthService`).
2. Remover ou desabilitar o `MockApiService` em `src/app/app.config.ts`.
3. Manter os modelos em `src/app/domain/models` e enums em `src/app/domain/enums` — eles refletem o contrato da API. Os nomes internos do front (`CenarioModel`, `ParametroModel` etc.) podem continuar iguais, mas os contratos de rede devem seguir os sufixos `Request`/`Response` definidos neste documento.

Exemplo de ajuste em `CenarioService`:

```ts
private readonly _baseUrl = `${environment.api.baseUrl}${environment.api.cenarios}`;
```

---

## 7. Checklist de controllers/endpoints a implementar

- [ ] `ContaController`
  - [ ] `GET /conta/profile`
- [ ] `CenariosController`
  - [ ] `GET /cenarios`
  - [ ] `GET /cenarios/{id}`
  - [ ] `POST /cenarios`
  - [ ] `PUT /cenarios/{id}`
  - [ ] `DELETE /cenarios/{id}`
  - [ ] `POST /cenarios/{id}/processar`
  - [ ] `GET /cenarios/{id}/metricas`
  - [ ] `GET /cenarios/{id}/semanas/{ano}/{semana}/pedidos`
  - [ ] `PATCH /cenarios/{id}/pedidos/mover`
  - [ ] `POST /cenarios/{id}/submeter`
- [ ] `ParametrosController`
  - [ ] `GET /parametros`
  - [ ] `GET /parametros/ativos`
  - [ ] `GET /parametros/{id}`
  - [ ] `POST /parametros`
  - [ ] `PUT /parametros/{id}`
  - [ ] `DELETE /parametros/{id}`
- [ ] `DemandasController`
  - [ ] `GET /demandas?cenarioId={id}`
  - [ ] `POST /demandas/upload`
