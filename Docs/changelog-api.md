# Changelog de mudanças na API — Otimizador de Pedidos

> Histórico de alterações no front-end que **exigem ajuste na API já implementada**.
> `docs/especificacao-api.md` é a especificação completa e sempre atualizada (é o
> "estado final" a implementar); este arquivo registra, a cada rodada de mudanças,
> **apenas o que mudou desde a última leitura** e o que precisa ser ajustado no
> backend em cima do que já está em produção/homologação. Entradas mais novas
> ficam no topo.
>
> Cada entrada é uma unidade de trabalho: descreve o recurso afetado, o
> contrato **antes** → **depois**, e uma ação objetiva a executar na API.

---

## 2026-08-03 — Critérios: chave tipada por enum + novo endpoint de listagem

Duas mudanças, ambas dentro do domínio de **critérios de otimização** (`CenariosController`,
prefixo `/cenarios`). Nenhuma outra controller (`ContaController`, `DemandasController`) é afetada.

### 1. Novo endpoint — `GET /cenarios/criterios-disponiveis`

**Ação: implementar endpoint novo.**

Até agora a lista de critérios disponíveis (usada para montar os badges na tela de criação de
cenário) vivia **hardcoded no front-end**. Ela passou a ser servida pela API.

| Método | Endpoint | Request | Response |
|---|---|---|---|
| GET | `/cenarios/criterios-disponiveis` | — | `CriterioDisponivelResponse[]` |

**Response `CriterioDisponivelResponse`** (novo schema):

```json
[
  {
    "chave": "tipoFrete",
    "nome": "Tipo de Frete",
    "tipo": "string"
  }
]
```

- `chave`: valor de `CriterioChaveEnum` (ver item 2 abaixo).
- `nome`: nome legível, exibido no badge e na tela de detalhes.
- `tipo`: valor de `TipoCriterioEnum` (`string` | `numerico`) — já existente na especificação,
  sem mudanças. Determina no front quais operadores ficam disponíveis para aquele critério.
- Hoje a lista tem um único item (`tipoFrete`). É uma lista **fixa, definida em código na API** —
  não crie CRUD/tabela para isso; ao adicionar um critério novo no futuro, ele entra nessa lista
  fixa (ver `docs/especificacao-api.md`, seção 5, item 9).
- Endpoint autenticado como os demais (JWT Bearer), sem parâmetros, sem paginação.

**⚠️ Atenção ao roteamento:** este endpoint tem o mesmo formato de rota que
`GET /cenarios/{id}` (`/cenarios/{segmento}`). A rota literal `criterios-disponiveis` **precisa
ter precedência** sobre o parâmetro dinâmico `{id}` no roteador da API — do contrário, uma
requisição a `/cenarios/criterios-disponiveis` seria capturada pelo handler de
`GET /cenarios/{id}` (tentando buscar um cenário com id `"criterios-disponiveis"`) em vez de
retornar a lista de critérios. Garanta que a rota específica seja registrada/avaliada antes da
rota com parâmetro (no mock de referência do front, isso foi resolvido registrando o handler de
`criterios-disponiveis` antes do handler de `:id` — replique a mesma precedência no framework de
rotas da API real).

Referência completa: `docs/especificacao-api.md`, seções 2.2 e 3.10.1.

---

### 2. Contrato alterado — `criterioChave` deixa de ser `string` livre e passa a ser enum fechado

**Ação: ajustar validação/schema nos endpoints já implementados abaixo.**

O campo `criterioChave`, usado dentro de cada item da lista `criterios`, era documentado/validado
como `string` livre (ex.: `"tipoFrete"` digitado sem verificação). Passou a ser um enum fechado,
`CriterioChaveEnum`:

```json
// CriterioChaveEnum
{
  "tipoFrete": "Critério \"Tipo de Frete\". Hoje o único critério implementado."
}
```

**Antes:**
```json
{ "criterioChave": "string", "operador": "OperadorCriterioEnum", "valor": "string", "peso": "number" }
```

**Depois:**
```json
{ "criterioChave": "CriterioChaveEnum", "operador": "OperadorCriterioEnum", "valor": "string", "peso": "number" }
```

Endpoints que **recebem** `criterioChave` no corpo da requisição (dentro de `criterios[]`) e
precisam passar a validar contra o enum, rejeitando qualquer valor fora dele com
`400 Bad Request`:

- `POST /cenarios` — `CenarioCriacaoRequest.criterios[].criterioChave`
- `PUT /cenarios/{id}` — `CenarioAtualizacaoRequest.criterios[].criterioChave`

Endpoints que **retornam** `criterioChave` no corpo da resposta (dentro de `criterios[]`, aninhado
em `CenarioDetalheResponse`) — apenas trocar a documentação/tipagem do campo, sem mudança de
comportamento (o valor devolvido já era `"tipoFrete"`, isso só formaliza que não pode ser outra coisa):

- `GET /cenarios/{id}`
- `POST /cenarios/{id}/csv`
- `POST /cenarios/{id}/processar`
- `POST /cenarios/{id}/submeter`
- `PUT /cenarios/{id}` (resposta, além do request acima)

**Não afetados** (não carregam `criterios`/`criterioChave`): `GET /cenarios` (`CenarioListaResponse`
não inclui critérios) e a resposta de `POST /cenarios` (`CenarioCriacaoResponse` só tem `id`).

Referência completa: `docs/especificacao-api.md`, seções 2.2, 3.1 (`CriterioChaveEnum`), 3.9 e 3.10.

---

## Checklist desta rodada

- [ ] Implementar `GET /cenarios/criterios-disponiveis` (com precedência de rota sobre `GET /cenarios/{id}`)
- [ ] Criar/expor o enum `CriterioChaveEnum` no backend (hoje: só `tipoFrete`)
- [ ] Validar `criterioChave` contra `CriterioChaveEnum` em `POST /cenarios` e `PUT /cenarios/{id}` (`400 Bad Request` se inválido)
- [ ] Atualizar a documentação/schema (Swagger/OpenAPI etc.) dos endpoints listados no item 2 para refletir `criterioChave: CriterioChaveEnum`
