# Especificação da API — Otimizador V2 (`OtimizadorV2Service`)

> Complementa `Docs/especificacao-api.md` (documento geral da API). Este documento cobre **apenas**
> os 3 endpoints novos do motor V2, para servir de referência à implementação do módulo V2 no
> front-end (Angular). Mantém o mesmo estilo/convenções do documento geral.

---

## 1. Visão geral

O motor V2 é uma **segunda estrutura de otimização**, independente da V1 (`/cenarios/{id}/otimizar`)
e do processamento simples (`/cenarios/{id}/processar`). Não substitui nenhum dos dois — é um módulo
novo, com suas próprias tabelas e endpoints, pensado para PoC.

Diferenças-chave em relação ao que já existe:

- **Critérios personalizados como objetivo matemático**: em vez de pesos fixos no código, o V2 lê a
  lista de critérios já configurada no cenário (`criterios`, a mesma lista enviada em
  `POST`/`PUT /cenarios` — ver §3.9/§3.10 do documento geral) e usa a soma dos pesos das regras que
  casam com cada item como prioridade no solver CP-SAT.
- **Granularidade por produto/planta**: cada "pedido" do V2 é uma alocação
  cliente + produto + centro (planta) + semana — mais fino que o pedido simples do V1/processamento
  atual, que é só cliente + semana. Por isso os pedidos do V2 vivem numa tabela própria
  (`PedidoV2`), sem relação com `PedidoResponse`/`GET /cenarios/{id}/semanas/{ano}/{semana}/pedidos`
  já existentes.
- **Pinning**: um pedido V2 movido manualmente (`PATCH .../pedidos/mover`) fica marcado como
  `pinado`. Numa nova chamada de `POST /otimizar-v2`, pedidos pinados **não são recalculados** — o
  volume e a capacidade que eles já ocupam são descontados antes de reotimizar o restante.

**Base URL, autenticação, formato de data e padrão de IDs**: iguais ao restante da API — ver §1 do
documento geral (`Docs/especificacao-api.md`).

---

## 2. Endpoints — `CenariosController`, prefixo `/cenarios`

| Método | Endpoint | Descrição | Request | Response |
|---|---|---|---|---|
| POST | `/cenarios/{id}/otimizar-v2` | Roda a otimização CP-SAT V2 para o cenário e persiste o resultado. | `OtimizacaoV2Request` (opcional — pode enviar `{}` ou body vazio) | `OtimizacaoV2Response` |
| GET | `/cenarios/{id}/otimizar-v2/semanas/{ano}/{semana}/pedidos` | Lista os pedidos V2 de uma semana específica do cenário. **Este é o endpoint para a tela de visualização por semana.** | — | `PedidoV2Response[]` |
| PATCH | `/cenarios/{id}/otimizar-v2/pedidos/mover` | Move um pedido V2 para outra semana e o marca como `pinado`. | `MoverPedidoV2Request` | `PedidoV2Response` |

Padrão de uso: `POST /otimizar-v2` roda o algoritmo e grava os pedidos; a tela de visualização
semanal consome `GET .../semanas/{ano}/{semana}/pedidos` para exibi-los (igual ao fluxo já existente
para `Pedido` normal, só que no namespace `/otimizar-v2`); arrastar/mover um pedido na tela chama
`PATCH .../pedidos/mover`, que fixa o pedido para a próxima reotimização.

#### Regras de negócio

- `POST /otimizar-v2` retorna `404 Not Found` se o cenário não existir, e `400 Bad Request` se o
  cenário não tiver nenhuma demanda carregada (mesmo texto de erro do V1: "Cenário sem demandas
  carregadas").
- Cada chamada de `POST /otimizar-v2` **substitui** os pedidos V2 não-pinados do cenário (apaga e
  recria) e **preserva** os pedidos pinados — mesmo contrato que `POST /cenarios/{id}/processar` já
  usa para `Pedido.pinado`.
- `PATCH .../pedidos/mover` retorna `404 Not Found` se `pedidoId` não existir (ou não pertencer ao
  cenário da URL).

---

## 3. Modelos (schemas)

### 3.1 `OtimizacaoV2Request`

Enviado em `POST /cenarios/{id}/otimizar-v2`. **Todos os campos são opcionais** — pode enviar `{}`
(ou nem enviar body) para usar os defaults do motor.

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

| Campo | Tipo | Default se omitido | Descrição |
|---|---|---|---|
| `horizonte` | `number \| null` | `8` | Quantidade de semanas à frente consideradas na otimização. |
| `capacidade` | `number \| null` | `1` (Simulada) | Modo de capacidade — ver tabela `ModoCapacidade` abaixo. |
| `semanaInicial` | `string \| null` | semana corrente | Semana ISO de início do horizonte, formato `"2026-W32"`. |
| `alvoCapacidadeSobreDemanda` | `number \| null` | `0.8` | Fração alvo de capacidade sobre a demanda elegível (0–1). |
| `limiteSegundos` | `number \| null` | `60` | Tempo máximo do solver CP-SAT, em segundos. |
| `carretaMinimoM3` | `number \| null` | `25` | Volume mínimo (m³) para formar um embarque/carreta. |
| `carretaMaximoM3` | `number \| null` | `30` | Volume máximo (m³) por carreta. |
| `limiteRecebimentoCarretasPorSemana` | `number \| null` | desativado | Quando enviado, ativa o limite de nº de carretas que um mesmo cliente pode receber por semana. |

**`ModoCapacidade`** (campo `capacidade`, inteiro):

| Valor | Significado |
|---|---|
| `0` | Real — usa a capacidade real do tático apenas nas semanas em que ela existe. |
| `1` | Simulada (default) — replica o perfil médio semanal mais recente para o horizonte todo. |
| `2` | Espalhada — redistribui a capacidade real entre plantas elegíveis para a mesma linha de produto. |

### 3.2 `OtimizacaoV2Response`

Retornado em `POST /cenarios/{id}/otimizar-v2`.

```json
{
  "resultadoId": "A1B2C3",
  "geradoEm": "2026-08-07T14:32:10Z",
  "horizonte": ["2026-W32", "2026-W33", "2026-W34", "2026-W35"],
  "solver": { "...": "OtimizacaoV2SolverResponse" },
  "resumo": { "...": "OtimizacaoV2ResumoResponse" },
  "alocacoes": ["OtimizacaoV2AlocacaoResponse[]"],
  "naoAlocado": ["OtimizacaoV2NaoAlocadoResponse[]"],
  "notas": ["string[]"]
}
```

| Campo | Tipo | Descrição |
|---|---|---|
| `resultadoId` | `string` | Identificador da execução (6 caracteres, mesmo padrão de ID da API). |
| `geradoEm` | `Date (ISO 8601)` | Data/hora (UTC) em que a otimização rodou. |
| `horizonte` | `string[]` | Lista de semanas ISO consideradas, ex.: `"2026-W32"`. |
| `solver` | `OtimizacaoV2SolverResponse` | Estatísticas do solver CP-SAT (ver 3.3). |
| `resumo` | `OtimizacaoV2ResumoResponse` | Números agregados da execução (ver 3.4). |
| `alocacoes` | `OtimizacaoV2AlocacaoResponse[]` | Pedidos **recém-gerados** nesta execução (não inclui os que já estavam pinados de execuções anteriores — esses continuam intactos e aparecem normalmente em `GET .../semanas/{ano}/{semana}/pedidos`). |
| `naoAlocado` | `OtimizacaoV2NaoAlocadoResponse[]` | Saldos de demanda que não couberam na capacidade disponível do horizonte. |
| `notas` | `string[]` | Mensagens de diagnóstico em texto livre (calibração de capacidade, avisos de pré-flight, resumo do pinning aplicado etc.) — útil para um painel de detalhes/debug, não para regra de negócio. |

### 3.3 `OtimizacaoV2SolverResponse`

```json
{
  "status": "Optimal",
  "segundos": 3.271,
  "objetivo": 1840,
  "variaveis": 512,
  "binarias": 340
}
```

| Campo | Tipo | Descrição |
|---|---|---|
| `status` | `string` | Status do CP-SAT: `"Optimal"`, `"Feasible"`, `"Infeasible"`, `"Unknown"` etc. (enum do Google OR-Tools, serializado como string). |
| `segundos` | `number` | Tempo de execução do solver. |
| `objetivo` | `number` | Valor da função objetivo (quanto menor, menos volume ponderado ficou sem atender — não tem unidade de negócio direta). |
| `variaveis` | `number` | Quantidade de variáveis do modelo CP-SAT. |
| `binarias` | `number` | Quantidade de variáveis binárias do modelo. |

> `status` diferente de `"Optimal"`/`"Feasible"` (ex.: `"Unknown"` por timeout) indica que o solver
> não confirmou uma solução — nesse caso `alocacoes` pode vir vazio mesmo havendo demanda. Vale a
> tela de detalhes exibir esse campo com destaque quando não for `"Optimal"`.

### 3.4 `OtimizacaoV2ResumoResponse`

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
| `demandaTotalM3` | `number` | Soma de todo o volume de demanda do cenário (m³), antes de qualquer filtro. |
| `demandaElegivelM3` | `number` | Volume que passou no pré-flight (produto conhecido, com elegibilidade e capacidade cadastradas). |
| `alocadoM3` | `number` | Volume total alocado — **inclui** o volume dos pedidos pinados de execuções anteriores + o volume recém-alocado nesta execução. |
| `naoAlocadoM3` | `number` | Volume que não coube na capacidade disponível nesta execução. |
| `capacidadeTotal` | `number` | Capacidade total do horizonte (já com o volume pinado descontado), na unidade das bases de capacidade do parquet. |
| `percentualAlocado` | `number` | `alocadoM3 / demandaElegivelM3`, entre `0` e `1` (multiplique por 100 para exibir como %). |
| `itens` | `number` | Quantidade de itens (agrupamentos cliente+produto) considerados. |
| `itensExcluidos` | `number` | Itens descartados no pré-flight (produto desconhecido, sem elegibilidade/capacidade, lote mínimo maior que a demanda). |

### 3.5 `OtimizacaoV2AlocacaoResponse`

Item da lista `alocacoes` em `OtimizacaoV2Response` — mesma forma de `PedidoV2Response` (3.7), sem o
`id` (ainda não persistido no momento da resposta) e sempre com `pinado: false` (é sempre uma
alocação nova).

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

### 3.6 `OtimizacaoV2NaoAlocadoResponse`

Item da lista `naoAlocado` em `OtimizacaoV2Response` — volume de um item (cliente+produto) que não
coube na capacidade do horizonte.

```json
{
  "cliente": "561125447",
  "material": "1741252",
  "linhaProdutoId": 17,
  "volumeM3": 4.83,
  "motivo": "sem capacidade suficiente no horizonte para o volume restante"
}
```

### 3.7 `PedidoV2Response` — endpoint de listagem por semana

Retornado em `GET /cenarios/{id}/otimizar-v2/semanas/{ano}/{semana}/pedidos` e em
`PATCH /cenarios/{id}/otimizar-v2/pedidos/mover`. **Este é o modelo a usar na tela de visualização
de pedidos do módulo V2.**

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
| `id` | `string` | Identificador do pedido V2 (6 caracteres). Usar como `pedidoId` em `MoverPedidoV2Request`. |
| `cliente` | `string` | Identificador do cliente (mesmo valor de `Demanda.cliente`). |
| `material` | `string` | Identificador do produto/material (mesmo valor de `Demanda.material`). **Não existe em `PedidoResponse` (V1/processamento simples)** — é novo do V2. |
| `linhaProdutoId` | `number` | Linha de produto do material, vinda da base de produtos (parquet). Novo do V2. |
| `centroId` | `number` | Identificador do centro/planta que vai produzir/atender o pedido. Novo do V2. |
| `centro` | `string` | Nome legível do centro (ex.: `"Jaguariaíva"`). Novo do V2. |
| `tipoFrete` | `string` | `"CIF"` ou `"FOB"` — mesma convenção de `PedidoResponse.tipoFrete`. |
| `volume` | `number` | Volume (m³) desta alocação específica (cliente+produto+centro+semana) — **não** é o volume total do cliente na semana (diferença importante em relação a `PedidoResponse`, que é um agregado por cliente+semana; um mesmo cliente pode ter vários `PedidoV2` na mesma semana, um por produto/centro). |
| `ano` | `number` | Ano ISO da semana de entrega. |
| `semana` | `number` | Número da semana ISO de entrega. |
| `pinado` | `boolean` | Se `true`, este pedido foi fixado manualmente e não será alterado na próxima `POST /otimizar-v2`. |
| `scorePeso` | `number` | Soma dos pesos dos critérios personalizados que casaram com este item (ex.: TipoFrete=CIF com peso 15 → `15`). Útil para explicar/ordenar por prioridade na tela — quanto maior, mais prioritário foi este item para o solver. |

> **Diferença de granularidade vs. `PedidoResponse` (V1/processamento simples):** um `PedidoResponse`
> representa todo o volume de um cliente numa semana (agregado, sem produto/planta). Um `PedidoV2Response`
> representa uma alocação cliente+produto+centro+semana — a lista de pedidos de uma semana no V2
> normalmente terá **mais itens** que a lista equivalente do V1 para o mesmo cenário/semana, porque
> cada combinação de produto/planta vira uma linha própria. Se a tela for reaproveitada, agrupar por
> `cliente` (e opcionalmente exibir o detalhamento por `material`/`centro` num nível expansível) é o
> caminho mais direto para uma visualização parecida com a atual.

### 3.8 `MoverPedidoV2Request`

Enviado em `PATCH /cenarios/{id}/otimizar-v2/pedidos/mover`.

```json
{
  "pedidoId": "F4G5H6",
  "anoDestino": 2026,
  "semanaDestino": 34
}
```

Mesma forma de `MoverPedidoRequest` (V1) — move o pedido para `anoDestino`/`semanaDestino` e marca
`pinado = true`. Retorna o `PedidoV2Response` atualizado.

---

## 4. Dependências que o módulo V2 usa (já existentes, mas mudaram)

O V2 não introduziu novos endpoints de configuração — ele consome a mesma lista de critérios e o
mesmo cadastro de demandas já usados pelo V1/processamento simples. Duas coisas mudaram nesses
contratos existentes e afetam diretamente como configurar/testar o V2:

1. **`GET /cenarios/criterios-disponiveis` agora retorna 2 itens, não 1**:

   ```json
   [
     { "chave": 1, "nome": "Tipo de Frete", "tipo": "string" },
     { "chave": 2, "nome": "Tipo de Cliente", "tipo": "string" }
   ]
   ```

   O critério `chave: 2` (`TipoCliente`) já pode ser usado nas regras de `criterios` de
   `POST`/`PUT /cenarios` (mesmo formato de `CriterioRegraRequest` do documento geral, §3.9) — o V2 o
   avalia comparando contra o novo campo `Segmento` da demanda (ver item 2). Exemplo de regra:
   `{ "criterioChave": 2, "operador": "igual_a", "valor": "INDUSTRIA", "peso": 25 }`.

2. **`DemandaResponse` ganhou o campo `segmento`, e o CSV de upload ganhou uma 6ª coluna opcional**:

   ```json
   {
     "id": "X9Y8Z7",
     "cliente": "string",
     "material": "string",
     "volume": "number",
     "dataEntregaDesejada": "Date (ISO 8601)",
     "tipoFrete": "string",
     "segmento": "string"
   }
   ```

   Formato do CSV (`POST /cenarios/{id}/csv` e `POST /demandas/upload`) passa a ser
   `Cliente,Material,Volume,DataEntrega,TipoFrete,Segmento` — a coluna `Segmento` é **opcional**
   (CSVs antigos de 5 colunas continuam funcionando, assumindo `REVENDA`). Valores reconhecidos:
   qualquer variação de `"INDUSTRIA"` vira `Industria`; qualquer outro valor (incluindo ausente) vira
   `Revenda`. **Atenção**: a base real de demandas (`Data/Datasets/demanda.parquet`) usa uma
   nomenclatura de segmento de negócio diferente (`ESPECIALISTA`, `MODULADO`, `HOTELARIA` etc.), não
   `INDUSTRIA`/`REVENDA` — para testar o critério "Tipo de Cliente" com resultado visível, o CSV de
   demanda precisa ter a coluna `Segmento` preenchida explicitamente com `INDUSTRIA` em algumas linhas.

---

## 5. Códigos HTTP

Mesma tabela do documento geral (§4) — sem exceções específicas do V2. Resumo relevante:

| Código | Quando |
|---|---|
| `200 OK` | `POST /otimizar-v2`, `GET .../pedidos`, `PATCH .../pedidos/mover` bem-sucedidos. |
| `400 Bad Request` | Cenário sem demandas carregadas ao chamar `POST /otimizar-v2`. |
| `404 Not Found` | Cenário ou pedido V2 não encontrado. |

---

## 6. Fluxo de uso recomendado (para a tela do módulo V2)

1. Cenário já criado e com critérios configurados (`POST /cenarios`, reaproveita a tela existente —
   agora com "Tipo de Cliente" como segunda opção de critério, ver §4.1).
2. Demandas carregadas (`POST /cenarios/{id}/csv` ou `POST /demandas/upload`), idealmente já com a
   coluna `Segmento` preenchida se o cenário usar o critério "Tipo de Cliente".
3. `POST /cenarios/{id}/otimizar-v2` — dispara a otimização. A tela pode mostrar `resumo` e `solver`
   da resposta imediatamente (não precisa esperar outra chamada).
4. `GET /cenarios/{id}/otimizar-v2/semanas/{ano}/{semana}/pedidos` — carrega a grade semanal (mesma
   navegação por semana que a tela V1 já tem, apontando para o namespace `/otimizar-v2`).
5. Usuário arrasta/edita um pedido → `PATCH /cenarios/{id}/otimizar-v2/pedidos/mover` → pedido volta
   com `pinado: true`.
6. Repetir o passo 3 quando quiser reotimizar — pedidos pinados (passo 5) permanecem intactos, o
   resto é recalculado ao redor deles.
