# Especificação da API — Arauco Otimizador de Pedidos

> Referência completa dos endpoints da API hoje implementada (`arauco-otimizador-api`), gerada a
> partir do código-fonte (controllers, modelos de domínio, entidades) após a unificação do motor de
> otimização em uma única versão. Objetivo: dar ao time de front-end (`arauco-otimizador-pedidos-app`)
> uma base precisa para planejar os ajustes necessários — a API mudou desde a última integração (ver
> §6 "Impacto no front-end atual").

---

## 1. Visão geral

- **Base URL padrão**: configurada em `src/environments/environment.ts` via `api.baseUrl`.
- **Prefixos de módulo** (definidos em `environment.api`):
  - `conta`: `/conta`
  - `cenarios`: `/cenarios` (inclui o motor de otimização, ver §2.3)
  - `demandas`: `/demandas`
  - `auth`: `/auth` — existe no `environment.api` do front, mas **não há endpoint correspondente na
    API hoje**; a autenticação é feita direto contra o Cognito via AWS Amplify (`cognito.service.ts`),
    não por um `AuthController`.
- **Autenticação**: JWT Bearer. O `authInterceptor` do front injeta o token de acesso do Cognito no
  header `Authorization: Bearer <token>` em todas as requisições que não sejam assets.
- **Formato de data**: `Date`/`ISO 8601` (o front trata como objetos `Date` do JavaScript).
- **Idioma**: português (pt-BR) nas mensagens de erro e labels.
- **Padrão de IDs**: identificadores são `string`, alfanumérico uppercase de 6 caracteres (ex.:
  `ABC123`, `A1B2C3`, `X9Y8Z7`) — exceto os IDs internos das listas `alocacoes`/`naoAlocado` do motor
  de otimização (não persistidos no momento da resposta, ver §3.20/§3.21).
- **Sufixos de modelos**: `*Request` (front → back), `*Response` (back → front) — um modelo por
  operação, nunca um modelo genérico reaproveitado entre list/get/criar/atualizar.
- **Critérios de otimização**: não existe cadastro global de critérios (sem `ParametrosController`/
  `CriteriosController`/`/parametros`). Os critérios disponíveis e seu tipo de dado (`TipoCriterioEnum`)
  são fixos em código. O que o usuário configura, por cenário, é uma **lista de regras** — critério +
  operador (`OperadorCriterioEnum`) + valor + peso —, enviada em `POST`/`PUT /cenarios` no campo
  `criterios`. O mesmo critério pode se repetir na lista. **Hoje existem 2 critérios**: "Tipo de Frete"
  e "Tipo de Cliente" (ver §2.2 e §3.1/3.9/3.10).
- **Motor de otimização**: um segundo mecanismo de geração de pedidos, além do processamento simples
  (`POST /cenarios/{id}/processar`) — roda um solver CP-SAT considerando os critérios do cenário e
  produz pedidos numa granularidade mais fina (cliente + produto + planta + semana). Endpoints
  próprios sob `/cenarios/{id}/otimizar...` — ver §2.3. As duas coisas coexistem: `processar` continua
  existindo e gerando `Pedido`/`PedidoResponse` como sempre gerou.
- **Health check**: `GET /check` (sem prefixo, `DefaultController`) — retorna `200 OK` vazio, uso de
  infraestrutura/monitoramento, sem necessidade de consumo pelo front.
- **Módulo fora deste produto**: o código-fonte também tem um `CartaoController` (`/cartao`) —
  mantido de propósito como exemplo de referência da arquitetura-base do repositório, **não faz parte
  do domínio "Otimizador de Pedidos"** e não deve ser integrado por este front-end.

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

> Consumido em `AuthService._loadProfile()` (`src/app/core/auth/auth.service.ts`).

---

### 2.2 `CenariosController` — prefixo `/cenarios`

Controller principal do domínio: CRUD de cenário, upload/download de demandas via CSV, e o
processamento simples (agrupamento por cliente+semana).

| Método | Endpoint | Descrição | Request | Response |
|---|---|---|---|---|
| GET | `/cenarios` | Lista todos os cenários. | — | `CenarioListaResponse[]` |
| GET | `/cenarios/criterios-disponiveis` | Lista os critérios disponíveis (lista fixa em código) — usada pelo front para montar os badges de critério na tela de criação. | — | `CriterioDisponivelResponse[]` |
| GET | `/cenarios/{id}` | Retorna um cenário pelo identificador. | — | `CenarioDetalheResponse` |
| POST | `/cenarios` | Cria um novo cenário (nome e regras de critério — sem arquivo). | `CenarioCriacaoRequest` | `CenarioCriacaoResponse` |
| PUT | `/cenarios/{id}` | Atualiza um cenário existente. | `CenarioAtualizacaoRequest` | `CenarioDetalheResponse` |
| DELETE | `/cenarios/{id}` | Remove um cenário e seus dados relacionados (demandas/pedidos). | — | `204 No Content` |
| POST | `/cenarios/{id}/csv` | Envia o arquivo CSV de demandas do cenário. Permitido **apenas uma vez** por cenário. | `multipart/form-data` — campo `arquivo` (`.csv`) | `CenarioDetalheResponse` |
| GET | `/cenarios/{id}/csv` | Baixa o arquivo CSV de demandas previamente carregado. | — | Arquivo (`text/csv`) |
| POST | `/cenarios/{id}/processar` | Processamento **simples**: agrupa demandas por cliente+semana e gera pedidos (`Pedido`). | `{}` (body vazio) | `CenarioDetalheResponse` |
| GET | `/cenarios/{id}/metricas` | Retorna as métricas de processamento do cenário. | — | `CenarioMetricasResponse` |
| GET | `/cenarios/{id}/semanas/{ano}/{semana}/pedidos` | Pedidos (fluxo **simples**) de uma semana do cenário. | — | `PedidoResponse[]` |
| PATCH | `/cenarios/{id}/pedidos/mover` | Move um pedido (fluxo **simples**) para outra semana (fixando-o). | `MoverPedidoRequest` | `PedidoResponse` |
| POST | `/cenarios/{id}/submeter` | Submete o cenário processado ao sistema externo (SAP, futuramente). | `{}` (body vazio) | `CenarioDetalheResponse` |

> `CenarioListaResponse` (listagem) e `CenarioDetalheResponse` (detalhe/mutações) têm campos
> diferentes — ver §3.2/§3.3.

#### Regras de negócio

- `GET /cenarios/criterios-disponiveis` retorna a **lista fixa de critérios disponíveis** (sem
  CRUD/tabela). O `criterioChave` usado nas regras (`POST`/`PUT /cenarios`) precisa ser um dos
  valores desse enum (`CriterioChaveEnum`, §3.1) — valores fora dele são rejeitados com
  `400 Bad Request`. A rota literal `criterios-disponiveis` tem precedência sobre `GET /cenarios/{id}`.
- Ao criar (`POST /cenarios`): persiste o cenário com status `pendente` e `arquivoNome = null`;
  persiste a lista de regras de critério junto ao próprio cenário (não é cadastro à parte); retorna
  só o identificador criado.
- Upload de CSV (`POST /cenarios/{id}/csv`): rejeitado com `409 Conflict` se o cenário já tiver
  arquivo carregado — **só é permitido uma vez por cenário**, sem substituição posterior. Formato do
  CSV: ver §5, item 4.
- `POST /cenarios/{id}/processar` (fluxo simples) exige que o cenário já tenha demandas carregadas
  (`400 Bad Request` caso contrário: "Cenário sem demandas carregadas"); agrupa por cliente+semana,
  atualiza `status = processado`/`dataUltimoProcessamento`/`primeiraSemana`/`ultimaSemana`.
- `POST /cenarios/{id}/submeter`: marca `submetido = true` e `status = submetido`.
- `PATCH /cenarios/{id}/pedidos/mover` (fluxo simples): atualiza `ano`/`semana` do pedido e marca
  `pinado = true`.
- `PedidoResponse` não inclui `cenarioId` (dado pela URL) nem `grupo` (uso interno).

---

### 2.3 `CenariosController` — motor de otimização, prefixo `/cenarios/{id}/otimizar`

Um **segundo mecanismo** de geração de pedidos, independente do processamento simples acima: roda um
solver CP-SAT sobre a carteira de demandas do cenário e a capacidade disponível (master data nas
tabelas `Produto`/`Centro`/`Elegibilidade`/`Capacidade`/`Carteira`), gerando uma alocação
**cliente + produto + centro (planta) + semana** por pedido — mais fino que o fluxo simples, que é só
cliente + semana. Por isso os pedidos gerados por este motor vivem numa tabela própria
(`PedidoOtimizado`), sem relação com `PedidoResponse`/`/semanas/.../pedidos` do §2.2.

Características do motor:

- **Critérios personalizados como objetivo matemático**: usa a mesma lista `criterios` do cenário
  (§2.2/§3.9) — soma os pesos das regras que casam com cada item como prioridade no solver.
- **Granularidade por produto/planta**: cada "pedido" é uma alocação cliente + produto + centro + semana.
- **Pinning**: um pedido movido manualmente (`PATCH .../pedidos/mover`) fica marcado `pinado`. Numa
  nova chamada de `POST /otimizar`, pedidos pinados **não são recalculados** — volume e capacidade já
  ocupados por eles são descontados antes de reotimizar o restante.

| Método | Endpoint | Descrição | Request | Response |
|---|---|---|---|---|
| POST | `/cenarios/{id}/otimizar` | Roda a otimização CP-SAT para o cenário e persiste o resultado. | `OtimizacaoRequest` (opcional — `{}` ou body vazio) | `OtimizacaoResponse` |
| GET | `/cenarios/{id}/otimizar/semanas/{ano}/{semana}/pedidos` | Lista os pedidos do motor numa semana específica. **Endpoint da tela de visualização por semana.** | — | `PedidoOtimizadoResponse[]` |
| PATCH | `/cenarios/{id}/otimizar/pedidos/mover` | Move um pedido do motor para outra semana e o marca `pinado`. | `MoverPedidoOtimizadoRequest` | `PedidoOtimizadoResponse` |

Padrão de uso: `POST /otimizar` roda o algoritmo e grava os pedidos; a tela semanal consome
`GET .../semanas/{ano}/{semana}/pedidos`; arrastar/mover um pedido chama `PATCH .../pedidos/mover`,
que fixa o pedido para a próxima reotimização.

#### Regras de negócio

- `POST /otimizar` retorna `404 Not Found` se o cenário não existir, e `400 Bad Request` se não
  houver nenhuma demanda carregada (mesmo texto de erro do fluxo simples).
- Cada chamada de `POST /otimizar` **substitui** os pedidos não-pinados do cenário (apaga e recria) e
  **preserva** os pinados.
- `PATCH .../pedidos/mover` retorna `404 Not Found` se `pedidoId` não existir (ou não pertencer ao
  cenário da URL).

---

### 2.4 `DemandasController` — prefixo `/demandas`

Responsável pelo gerenciamento das demandas importadas via CSV.

| Método | Endpoint | Descrição | Request | Response |
|---|---|---|---|---|
| GET | `/demandas?cenarioId={id}` | Lista as demandas de um cenário. | Query param `cenarioId` | `DemandaResponse[]` |
| POST | `/demandas/upload` | Faz upload/reimportação de demandas via CSV para um cenário. | `DemandaUploadRequest` | `DemandaResponse[]` |

#### Regras de negócio

- `GET /demandas` requer o query parameter `cenarioId`.
- `POST /demandas/upload` substitui as demandas existentes do cenário pelas novas, faz o parse do CSV
  (formato: §5, item 4) e retorna a nova lista.
- `DemandaResponse` não repete `cenarioId` (já informado via query param/payload).

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

Chave fechada dos critérios disponíveis. `criterioChave` é tipado como este enum e **transmitido como
inteiro**. Valores fora dele são rejeitados com `400 Bad Request`.

| Valor (int) | Membro | Descrição |
|---|---|---|
| `1` | `TipoFrete` | Critério "Tipo de Frete" — compara contra `CIF`/`FOB`. |
| `2` | `TipoCliente` | Critério "Tipo de Cliente" — compara contra um valor derivado (`INDUSTRIA`/`REVENDA`) do campo `segmento` da demanda: `INDUSTRIA` se o texto do segmento contiver "IND", `REVENDA` caso contrário (mesma heurística do projeto de referência `otimizador-teste-entrega`). Os segmentos reais da extração do ADC (`ESPECIALISTA`, `MAYORISTA`, `EXPORTAÇÃO` etc.) não contêm "IND", então hoje toda demanda real resolve para `REVENDA` — heurística preservada de propósito para manter paridade com a referência; nenhum mapeamento oficial segmento→Indústria/Revenda existe ainda para esses valores. |

#### `TipoCriterioEnum`

| Valor | Descrição |
|---|---|
| `string` | Critério cujo valor de comparação é textual (Tipo de Frete e Tipo de Cliente, hoje, são ambos `string`). |
| `numerico` | Critério cujo valor de comparação é numérico (nenhum critério numérico implementado ainda). |

> Não é enviado/recebido via API — qualifica os critérios de `GET /cenarios/criterios-disponiveis`.

#### `OperadorCriterioEnum`

| Valor | Descrição | Aplicável a |
|---|---|---|
| `igual_a` | Igual a. | `string` e `numerico` |
| `diferente_de` | Diferente de. | `string` e `numerico` |
| `maior_que` | Maior que. | `numerico` |
| `menor_que` | Menor que. | `numerico` |
| `comeca_com` | Começa com. | `string` |
| `termina_com` | Termina com. | `string` |

#### `ModoCapacidade` (só no motor de otimização, campo `OtimizacaoRequest.capacidade`)

| Valor (int) | Significado |
|---|---|
| `0` | Real — usa a capacidade real apenas nas semanas em que ela existe. |
| `1` | Simulada (default) — replica o perfil médio semanal mais recente para o horizonte todo. |
| `2` | Espalhada — redistribui a capacidade real entre plantas elegíveis para a mesma linha de produto. |

---

### 3.2 `CenarioListaResponse`

Retornado em `GET /cenarios`. Forma resumida — não inclui `status`, `arquivoNome`, `criterios` nem
`primeiraSemana`/`ultimaSemana`.

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

Retornado em `GET /cenarios/{id}` e, por representarem o mesmo recurso atualizado, também em
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

Enviado em `POST /cenarios`. Não contém dados do arquivo — upload do CSV é uma requisição separada.

```json
{
  "nome": "string",
  "criterios": ["CriterioRegraRequest[]"]
}
```

### 3.4.1 `CenarioCriacaoResponse`

```json
{ "id": "ABC123" }
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

Corpo `multipart/form-data` com um único campo de arquivo.

| Campo | Tipo | Descrição |
|---|---|---|
| `arquivo` | `file` (`.csv`) | Arquivo de demandas no formato descrito em §5, item 4. |

### 3.5 `CenarioMetricasResponse`

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
{ "ano": "number", "semana": "number", "volume": "number", "quantidadePedidos": "number" }
```

### 3.7 `CenarioOcupacaoPlantaResponse`

```json
{ "data": "Date (ISO 8601)", "percentual": "number (0-100)" }
```

### 3.8 `SemanaAnoResponse`

```json
{ "ano": "number", "semana": "number" }
```

### 3.9 `CriterioRegraRequest`

Item da lista `criterios` em `CenarioCriacaoRequest`/`CenarioAtualizacaoRequest`. `peso` é um inteiro
de -100 a 100 (negativo penaliza, positivo prioriza). O mesmo `criterioChave` pode se repetir.

```json
{
  "criterioChave": 1,
  "operador": "OperadorCriterioEnum",
  "valor": "string",
  "peso": "number"
}
```

### 3.10 `CriterioRegraResponse`

Mesma forma de `CriterioRegraRequest`, retornado em `CenarioDetalheResponse.criterios`.

```json
{ "criterioChave": 1, "operador": "OperadorCriterioEnum", "valor": "string", "peso": "number" }
```

### 3.10.1 `CriterioDisponivelResponse`

Retornado em `GET /cenarios/criterios-disponiveis`. **Hoje retorna 2 itens** (mudou de 1 para 2 desde
a última integração — ver §6):

```json
[
  { "chave": 1, "nome": "Tipo de Frete", "tipo": "string" },
  { "chave": 2, "nome": "Tipo de Cliente", "tipo": "string" }
]
```

### 3.11 `DemandaResponse`

Retornado em `GET /demandas?cenarioId={id}` e `POST /demandas/upload`. Os campos espelham a extração
"carteira em aberto" do ADC (mesmo formato de `otimizador-teste-entrega/sql/extracao/demanda.sql`, ver
§5 item 4):

```json
{
  "id": "X9Y8Z7",
  "carteiraId": "number",
  "cliente": "string",
  "clienteNome": "string",
  "material": "string",
  "linhaProdutoId": "number",
  "volume": "number",
  "dataDocumento": "Date (ISO 8601)",
  "dataEntregaDesejada": "Date (ISO 8601)",
  "tipoFrete": "string",
  "segmento": "string",
  "centroOriginal": "number"
}
```

> `tipoFrete` e `segmento` são texto livre, não enums fechados. `tipoFrete` é derivado do `incoterms` do
> arquivo (`CIF` se começar com "CIF", `FOB` caso contrário — CIP/DAP/FCA também caem em `FOB` por essa
> regra, mesmo critério do motor de otimização). `segmento` é o texto bruto do arquivo (`ESPECIALISTA`,
> `MAYORISTA`, `EXPORTAÇÃO` etc.) — ver critério "Tipo de Cliente" em §3.1/§2.3 para como ele é
> normalizado para `INDUSTRIA`/`REVENDA`. `dataDocumento` é a data do documento de origem (usada pelo
> motor de otimização para o critério de antiguidade); `dataEntregaDesejada` é a data solicitada de
> remessa pelo cliente (usada apenas pelo fluxo simples de agrupamento por semana — `POST /processar`;
> o motor CP-SAT não a utiliza, mesma decisão do projeto de referência). `centroOriginal` é o centro
> onde o pedido está hoje na carteira real — meramente informativo, o motor de otimização decide o
> centro de destino livremente e ignora este campo.

### 3.12 `DemandaUploadRequest`

```json
{ "cenarioId": "ABC123", "conteudoCsv": "string" }
```

### 3.13 `PedidoResponse` (fluxo simples)

Retornado em `GET /cenarios/{id}/semanas/{ano}/{semana}/pedidos` e `PATCH /cenarios/{id}/pedidos/mover`.
Agregado por **cliente + semana** (sem produto/planta). Não inclui `cenarioId` nem `grupo`.

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

### 3.14 `MoverPedidoRequest` (fluxo simples)

```json
{ "pedidoId": "X1Y2Z3", "anoDestino": "number", "semanaDestino": "number" }
```

### 3.15 `PerfilResponse`

```json
{ "colaboradorId": "ABC123", "nome": "string", "email": "string" }
```

---

### 3.16 `OtimizacaoRequest`

Enviado em `POST /cenarios/{id}/otimizar`. **Todos os campos são opcionais** — `{}` ou body vazio usa
os defaults do motor.

```json
{
  "horizonte": 8,
  "capacidade": 1,
  "semanaInicial": "2026-W32",
  "alvoCapacidadeSobreDemanda": 0.8,
  "limiteSegundos": 60,
  "carretaMinimoM3": 25,
  "carretaMaximoM3": 30,
  "limiteRecebimentoCarretasPorSemana": null
}
```

| Campo | Tipo | Default | Descrição |
|---|---|---|---|
| `horizonte` | `number \| null` | `8` | Semanas à frente consideradas. |
| `capacidade` | `number \| null` | `1` (Simulada) | Modo de capacidade — ver `ModoCapacidade`, §3.1. |
| `semanaInicial` | `string \| null` | semana corrente | Semana ISO de início, formato `"2026-W32"`. |
| `alvoCapacidadeSobreDemanda` | `number \| null` | `0.8` | Fração alvo de capacidade sobre a demanda elegível (0–1). |
| `limiteSegundos` | `number \| null` | `60` | Tempo máximo do solver, em segundos. |
| `carretaMinimoM3` | `number \| null` | `25` | Volume mínimo (m³) por embarque/carreta. |
| `carretaMaximoM3` | `number \| null` | `30` | Volume máximo (m³) por carreta. |
| `limiteRecebimentoCarretasPorSemana` | `number \| null` | desativado | Limite de carretas/semana por cliente, quando enviado. |

### 3.17 `OtimizacaoResponse`

Retornado em `POST /cenarios/{id}/otimizar`.

```json
{
  "resultadoId": "A1B2C3",
  "geradoEm": "2026-08-07T14:32:10Z",
  "horizonte": ["2026-W32", "2026-W33", "2026-W34", "2026-W35"],
  "solver": { "...": "OtimizacaoSolverResponse" },
  "resumo": { "...": "OtimizacaoResumoResponse" },
  "alocacoes": ["OtimizacaoAlocacaoResponse[]"],
  "naoAlocado": ["OtimizacaoNaoAlocadoResponse[]"],
  "notas": ["string[]"]
}
```

| Campo | Tipo | Descrição |
|---|---|---|
| `resultadoId` | `string` | Identificador da execução (6 caracteres). |
| `geradoEm` | `Date (ISO 8601)` | Data/hora (UTC) da execução. |
| `horizonte` | `string[]` | Semanas ISO consideradas, ex.: `"2026-W32"`. |
| `solver` | `OtimizacaoSolverResponse` | Estatísticas do solver (§3.18). |
| `resumo` | `OtimizacaoResumoResponse` | Números agregados (§3.19). |
| `alocacoes` | `OtimizacaoAlocacaoResponse[]` | Pedidos **recém-gerados** nesta execução (pinados de execuções anteriores não aparecem aqui, mas continuam em `GET .../semanas/{ano}/{semana}/pedidos`). |
| `naoAlocado` | `OtimizacaoNaoAlocadoResponse[]` | Saldos de demanda sem capacidade no horizonte. |
| `notas` | `string[]` | Mensagens de diagnóstico (calibração, pré-flight, pinning) — para painel de detalhes/debug. |

### 3.18 `OtimizacaoSolverResponse`

```json
{ "status": "Optimal", "segundos": 3.271, "objetivo": 1840, "variaveis": 512, "binarias": 340 }
```

| Campo | Tipo | Descrição |
|---|---|---|
| `status` | `string` | `"Optimal"`, `"Feasible"`, `"Infeasible"`, `"Unknown"` etc. (OR-Tools, serializado como string). |
| `segundos` | `number` | Tempo de execução do solver. |
| `objetivo` | `number` | Valor da função objetivo (sem unidade de negócio direta). |
| `variaveis` | `number` | Variáveis do modelo CP-SAT. |
| `binarias` | `number` | Variáveis binárias do modelo. |

> `status` diferente de `"Optimal"`/`"Feasible"` indica que o solver não confirmou solução —
> `alocacoes` pode vir vazio mesmo havendo demanda. Vale destacar esse campo na UI quando não for
> `"Optimal"`.

### 3.19 `OtimizacaoResumoResponse`

```json
{
  "demandaTotalM3": 158920.06,
  "demandaElegivelM3": 135706.76,
  "alocadoM3": 108481.32,
  "naoAlocadoM3": 27225.44,
  "capacidadeTotal": 108572,
  "percentualAlocado": 0.7994,
  "itens": 8358,
  "itensExcluidos": 1545
}
```

| Campo | Tipo | Descrição |
|---|---|---|
| `demandaTotalM3` | `number` | Volume total de demanda do cenário (m³), antes de filtros. |
| `demandaElegivelM3` | `number` | Volume que passou no pré-flight. |
| `alocadoM3` | `number` | Volume total alocado — inclui pinados de execuções anteriores + o novo. |
| `naoAlocadoM3` | `number` | Volume sem capacidade disponível nesta execução. |
| `capacidadeTotal` | `number` | Capacidade total do horizonte (já com o pinado descontado). |
| `percentualAlocado` | `number` | `alocadoM3 / demandaElegivelM3`, entre `0` e `1`. |
| `itens` | `number` | Itens (cliente+produto) considerados. |
| `itensExcluidos` | `number` | Itens descartados no pré-flight. |

### 3.20 `OtimizacaoAlocacaoResponse`

Item de `alocacoes` em `OtimizacaoResponse` — mesma forma de `PedidoOtimizadoResponse` (§3.22), sem
`id` (ainda não persistido na resposta) e sempre `pinado: false`.

```json
{
  "cliente": "561125447",
  "material": "1741252",
  "linhaProdutoId": 17,
  "centroId": 2,
  "centro": "Jaguariaíva",
  "tipoFrete": "CIF",
  "volume": 30.0,
  "ano": 2026,
  "semana": 32,
  "pinado": false,
  "scorePeso": 115
}
```

### 3.21 `OtimizacaoNaoAlocadoResponse`

Item de `naoAlocado` em `OtimizacaoResponse` — volume de um item (cliente+produto) sem capacidade.

```json
{
  "cliente": "561125447",
  "material": "1741252",
  "linhaProdutoId": 17,
  "volumeM3": 4.83,
  "motivo": "sem capacidade suficiente no horizonte para o volume restante"
}
```

### 3.22 `PedidoOtimizadoResponse`

Retornado em `GET /cenarios/{id}/otimizar/semanas/{ano}/{semana}/pedidos` e
`PATCH /cenarios/{id}/otimizar/pedidos/mover`. **Modelo da tela de visualização do motor de
otimização.**

```json
{
  "id": "F4G5H6",
  "cliente": "561125447",
  "material": "1741252",
  "linhaProdutoId": 17,
  "centroId": 2,
  "centro": "Jaguariaíva",
  "tipoFrete": "CIF",
  "volume": 30.00,
  "ano": 2026,
  "semana": 32,
  "pinado": false,
  "scorePeso": 115
}
```

| Campo | Tipo | Descrição |
|---|---|---|
| `id` | `string` | Identificador do pedido (6 caracteres). Usar como `pedidoId` em `MoverPedidoOtimizadoRequest`. |
| `cliente` | `string` | Identificador do cliente (mesmo valor de `Demanda.cliente`). |
| `material` | `string` | Identificador do produto. **Não existe em `PedidoResponse`** (fluxo simples). |
| `linhaProdutoId` | `number` | Linha de produto do material. |
| `centroId` | `number` | Centro/planta que atende o pedido. |
| `centro` | `string` | Nome legível do centro (ex.: `"Jaguariaíva"`). |
| `tipoFrete` | `string` | `"CIF"` ou `"FOB"`. |
| `volume` | `number` | Volume (m³) desta alocação **específica** (cliente+produto+centro+semana) — não é o total do cliente na semana. |
| `ano` / `semana` | `number` | Semana ISO de entrega. |
| `pinado` | `boolean` | `true` = fixado manualmente, não muda na próxima `POST /otimizar`. |
| `scorePeso` | `number` | Soma dos pesos dos critérios que casaram com este item — quanto maior, mais prioritário para o solver. |

> **Granularidade vs. `PedidoResponse`:** um `PedidoResponse` (fluxo simples) é o volume de um cliente
> numa semana inteira (sem produto/planta). Um `PedidoOtimizadoResponse` é uma alocação
> cliente+produto+centro+semana — a lista de uma semana normalmente tem **mais itens**, porque cada
> combinação produto/planta é uma linha própria. Para uma visualização parecida com a do fluxo
> simples, agrupar por `cliente` na tela (com detalhamento por `material`/`centro` expansível) é o
> caminho mais direto.

### 3.23 `MoverPedidoOtimizadoRequest`

Enviado em `PATCH /cenarios/{id}/otimizar/pedidos/mover`. Mesma forma de `MoverPedidoRequest` (fluxo
simples) — move para `anoDestino`/`semanaDestino` e marca `pinado = true`.

```json
{ "pedidoId": "F4G5H6", "anoDestino": 2026, "semanaDestino": 34 }
```

---

## 4. Códigos HTTP esperados

| Código | Uso |
|---|---|
| `200 OK` | Leitura/atualização bem-sucedida (inclui `POST /otimizar`, `POST /processar`). |
| `201 Created` | Criação bem-sucedida (`POST /cenarios`). |
| `204 No Content` | Exclusão bem-sucedida (`DELETE /cenarios/{id}`). |
| `400 Bad Request` | Payload inválido, CSV malformado, regra de negócio não atendida (ex.: cenário sem demandas). Corpo com `message`. |
| `401 Unauthorized` | Token inválido ou expirado. O front faz sign-out. |
| `403 Forbidden` | Usuário sem permissão. |
| `404 Not Found` | Recurso não encontrado (cenário, pedido). |
| `409 Conflict` | Conflito de estado (ex.: reenviar CSV de cenário que já tem arquivo). |
| `500 Internal Server Error` | Erro inesperado. |
| `503 Service Unavailable` | Serviço indisponível. O front redireciona para `/server-error`. |

Corpo padrão de erro (`ErrorBuilder`, aplicado globalmente):

```json
{ "message": "string", "trace": "string", "knownError": true }
```

`failures` é incluído no lugar de `knownError` quando o erro é de validação de modelo
(`ModelValidationException`).

---

## 5. Pontos de atenção

1. **IDs**: todo identificador (`id`, `cenarioId`, `pedidoId`, `colaboradorId`, `resultadoId`) é
   `string` alfanumérico uppercase de 6 caracteres.
2. **Cálculo de semana ISO**: mesma regra ISO 8601 usada pelo front (`Utils.obterSemanaIso`) — tanto
   no fluxo simples quanto no motor de otimização.
3. **Dois algoritmos, dois conjuntos de pedidos**: `POST /processar` (agrupamento simples por
   cliente+semana, `Pedido`/`PedidoResponse`) e `POST /otimizar` (CP-SAT com critérios, granularidade
   cliente+produto+planta+semana, `PedidoOtimizado`/`PedidoOtimizadoResponse`) são independentes —
   rodar um não afeta os pedidos do outro. Confirmar com o produto qual fluxo a tela deve priorizar
   (ou se ambos continuam coexistindo na UI).
4. **Formato CSV de demandas**: extração "carteira em aberto" do ADC, mesmo formato de
   `otimizador-teste-entrega/sql/extracao/demanda.sql` (projeto de referência usado desde o início).
   Arquivo com cabeçalho; colunas identificadas pelo nome (qualquer ordem), case-insensitive:
   `carteira_id, cliente_id, cliente_nome, produto_id, linha_produto_id, volume_m3, data_documento,
   incoterms, segmento, centro_original, data_solicitacao_remessa`. As demais colunas da extração real
   (`mes, ano, carteira_m3, faturado_m3, status_credito, tipo_documento_venda, numero_pedido, vendedor,
   regiao, prioridade_remessa`) podem estar presentes no arquivo mas são ignoradas — o motor de
   otimização portado da referência não as consome (ver `analise/CONTINUIDADE.md` do projeto de
   referência). `cliente_id`/`produto_id`/`volume_m3`/`data_documento` são obrigatórias; uma linha sem
   alguma delas, com `volume_m3 <= 0` ou com `data_documento` inválida é descartada silenciosamente
   (mesmo critério do `Carregador` do motor). `data_solicitacao_remessa` ausente cai para
   `data_documento`. Campos entre aspas com vírgula (ex.: nome de cliente `"Empresa, S.A."`) são
   suportados (RFC 4180). `incoterms` deriva `tipoFrete` (`CIF` se começar com "CIF", senão `FOB`).
5. **`pinado`**: em ambos os fluxos, pedidos movidos manualmente permanecem fixos numa nova execução
   do algoritmo correspondente.
6. **`primeiraSemana`/`ultimaSemana`**: derivados dos pedidos do fluxo **simples** (`Pedido`), não do
   motor de otimização.
7. **Critérios não são um cadastro**: a lista `criterios` é dado do próprio cenário, não uma entidade
   à parte. Quais critérios existem (hoje: Tipo de Frete, Tipo de Cliente) e seu `TipoCriterioEnum`
   são fixos em código, servidos por `GET /cenarios/criterios-disponiveis`.
8. **Validação de operador por tipo**: a API valida que `operador` é compatível com o
   `TipoCriterioEnum` do critério referenciado por `criterioChave` — `400 Bad Request` caso contrário.
9. **Autenticação**: toda requisição deve enviar o JWT Bearer do Cognito no header `Authorization`.

---

## 6. Impacto no front-end atual — o que precisa ser ajustado

Levantamento feito lendo o código atual de `arauco-otimizador-pedidos-app` (`src/app/services/*.ts`)
contra a API descrita acima. Pontos concretos a planejar:

1. **`CenarioV2Service` (`cenario-v2.service.ts`) usa rotas que não existem mais.** Os 3 métodos
   (`otimizar`, `obterPedidosPorSemana`, `moverPedido`) chamam `/cenarios/{id}/otimizar-v2...` — a API
   unificou o motor em `/cenarios/{id}/otimizar` (sem `-v2`, ver §2.3). **O formato dos payloads não
   mudou campo a campo** (`OtimizacaoV2Request`/`Response`, `PedidoV2Response`,
   `MoverPedidoV2Request` no front têm a mesma forma de `OtimizacaoRequest`/`Response`,
   `PedidoOtimizadoResponse`, `MoverPedidoOtimizadoRequest` na API) — o ajuste é essencialmente trocar
   as 3 URLs; renomear os tipos TS (`app/domain/models/cenario-v2`, `app/domain/models/pedido-v2`)
   para tirar o "V2" é opcional/cosmético, já que não existe mais uma "V1" para diferenciar.
2. **`CenarioService.processar()` (`cenario.service.ts`) aponta para `/cenarios/{id}/otimizar-v2`, mas
   tipa a resposta como `CenarioDetalheResponse`.** Isso não bate com nenhum contrato real, nem o
   antigo nem o atual — `POST /otimizar` sempre retornou `OtimizacaoResponse`/`OtimizacaoV2Response`,
   nunca `CenarioDetalheResponse`. Pelo nome do método e pelo tipo de retorno, o mais provável é que
   ele devesse chamar o fluxo simples `POST /cenarios/{id}/processar` (que de fato retorna
   `CenarioDetalheResponse`) — vale confirmar a intenção original antes de corrigir, já que hoje as
   duas telas (`modules/cenarios` e `modules/cenarios-v2`) parecem ter ficado misturadas nesse ponto.
3. **`GET /cenarios/criterios-disponiveis` agora retorna 2 itens**, não 1 (`Tipo de Frete` +
   `Tipo de Cliente`, ver §3.10.1). Telas/lógica que assumam item único devem ser revisadas.
4. **`DemandaResponse` ganhou o campo `segmento`**, e o CSV de upload aceita uma 6ª coluna opcional
   `Segmento` (§3.11, §5 item 4). `demanda.model.ts` e a tela de upload/preview de CSV devem refletir
   isso, especialmente se a tela for exibir/editar o critério "Tipo de Cliente".
5. **Convivência de dois módulos** (`modules/cenarios` e `modules/cenarios-v2`): com o back-end agora
   tendo só uma versão do motor de otimização, vale decidir com o produto se as duas telas continuam
   como fluxos paralelos (processamento simples vs. motor CP-SAT) ou se uma delas deve ser
   descontinuada/unificada.
6. **`CartaoController`/`ContaController`**: `Conta` já está integrado (`auth.service.ts`) e continua
   igual — sem ajuste necessário. `Cartao` não é usado pelo front hoje e não faz parte deste produto —
   nenhuma ação necessária.

---

## 7. Checklist de ajustes no front-end

- [ ] `cenario-v2.service.ts`: trocar as 3 URLs de `/otimizar-v2` para `/otimizar`.
- [ ] `cenario.service.ts#processar()`: revisar se deveria chamar `/processar` (fluxo simples) em vez
      de `/otimizar-v2`, e ajustar o tipo de retorno esperado de acordo com a decisão.
- [ ] Atualizar `criterio.model.ts`/telas de critério para lidar com 2 critérios disponíveis
      (`Tipo de Frete`, `Tipo de Cliente`), não mais 1.
- [ ] Atualizar `demanda.model.ts` (campo `segmento`) e a tela de upload/preview de CSV (6ª coluna
      opcional `Segmento`).
- [ ] Decidir, com o produto, o destino de `modules/cenarios` vs. `modules/cenarios-v2` (fluxo simples
      vs. motor de otimização) agora que o back-end não versiona mais o motor.
- [ ] (Opcional/cosmético) Renomear tipos TS que ainda carregam sufixo "V2"
      (`OtimizacaoV2Request/Response`, `PedidoV2Response`, `MoverPedidoV2Request`) já que não há mais
      uma "V1" para diferenciar.
