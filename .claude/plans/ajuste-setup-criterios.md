# Plano: Reestruturar o módulo Setup (critérios, campos do formulário e descrições)

## Contexto
O CRUD de setup foi implementado com um conjunto de campos e critérios que não reflete a
necessidade do negócio. Precisamos:
1. Manter na **ordem de importância** apenas 5 critérios, com descrições salvas no banco.
2. Remover do formulário os inputs "Tipo de frete" e "Tipo de cliente" (e as flags que os habilitam).
3. Trazer de volta os campos de configuração operacional: volume mínimo/máximo da carreta,
   quantidade mínima de SKU por lote, capacidade máxima de recebimento por cliente e o slider
   de mix de tipo de frete (CIF ↔ FOB).

## Decisões de design

### 1. Critérios e descrições
A lista de critérios válidos passa a ser fechada e com **descrições padrão fixas em código**,
mas **persistidas no banco** por setup (coluna `Descricao` em `SetupOrdemImportancia`). Dessa forma
o backend garante que todo setup tenha a explicação do critério, e o frontend só exibe.

Critérios (ordem inicial sugerida, mas reordenável e ativável/desativável):

| Critério | Descrição padrão (português) |
|---|---|
| `PriorizarFreteCIF` | No momento de priorizar os pedidos, leva em consideração o tipo de frete do cliente, e, caso seja CIF, o pedido é priorizado para entrega com a maior antecedência possível. |
| `PiorizarClienteRevenda` | No momento de priorizar os pedidos, identifica clientes do segmento Revenda e reduz a prioridade de entrega desses pedidos. |
| `Antecipar` | Prioriza a antecipação das entregas, buscando alocar os pedidos nas semanas mais próximas possíveis, respeitando a data de entrega desejada. |
| `AtenderDemanda` | Busca maximizar o volume total de demanda atendida no cenário, priorizando o cumprimento da quantidade solicitada. |
| `PedidoMaisAntigo` | Considera a antiguidade do pedido como critério de desempate, dando prioridade aos pedidos registrados há mais tempo. |

### 2. Formulário de setup
Removemos `PriorizarTipoFrete`, `TipoFrete`, `PriorizarTipoCliente`, `TipoCliente`.
Adicionamos:
- `VolumeMinimoCarreta` (m³)
- `VolumeMaximoCarreta` (m³)
- `QuantidadeMinimaSkuPorLote`
- `CapacidadeMaximaRecebimentoCliente` (carretas/semana)
- `MixTipoFrete` (int 0..100, percentual de CIF; o restante é FOB)

Mantemos: `Nome`, `Descricao`, `PesoMinimoCarregamento`, `PercentualVariacaoMediaVenda`,
`QuantidadeMaximaTrocas`, `UtilizarToleranciaPeso`, `PermitirCarregarAbaixoPesoMinimo`.

### 3. Banco de dados
- Script `Script010 - Setup.sql` é alterado (ainda não foi aplicado em produção; é o script original
do CRUD, portanto podemos editar o schema de origem).
- `Setup`: remove colunas `PriorizarTipoFrete`, `TipoFreteId`, `PriorizarTipoCliente`, `TipoClienteId`;
  adiciona `VolumeMinimoCarreta`, `VolumeMaximoCarreta`, `QuantidadeMinimaSkuPorLote`,
  `CapacidadeMaximaRecebimentoCliente`, `MixTipoFrete`.
- `SetupOrdemImportancia`: adiciona `Descricao VARCHAR(500) NULL`.

## Escopo de trabalho

### F1 — Backend: enums e modelos (`Common.Domain`)
- **Editar** `Enums/Setup/CriterioOrdemEnum.cs`:
  - Manter apenas: `PriorizarFreteCIF`, `PiorizarClienteRevenda`, `Antecipar`, `AtenderDemanda`, `PedidoMaisAntigo`.
  - Manter `EnumMember` com chaves em snake_case (`priorizar_frete_cif`, `piorizar_cliente_revenda`, etc.).
- **Remover** `Enums/Setup/TipoFreteEnum.cs` e `Enums/Setup/TipoClienteEnum.cs` (não são mais usados no setup).
- **Editar** `Models/Setup/SetupOrdemImportanciaRequest.cs`:
  - Adicionar `public string? Descricao { get; set; }`.
- **Editar** `Models/Setup/SetupOrdemImportanciaResponse.cs`:
  - Adicionar `public string? Descricao { get; set; }`.
- **Editar** `Models/Setup/SetupCriacaoRequest.cs` e `SetupAtualizacaoRequest.cs`:
  - Remover `PriorizarTipoFrete`, `TipoFrete`, `PriorizarTipoCliente`, `TipoCliente`.
  - Adicionar `VolumeMinimoCarreta`, `VolumeMaximoCarreta`, `QuantidadeMinimaSkuPorLote`,
    `CapacidadeMaximaRecebimentoCliente`, `MixTipoFrete` (todos nullable `decimal?`/`int?`, exceto
    `MixTipoFrete` que pode ser `int?` 0..100).
- **Editar** `Models/Setup/SetupDetalheResponse.cs` e `Models/Setup/SetupListaResponse.cs`:
  - Mesma alteração de campos acima. `SetupListaResponse` não precisa de todos, mas mantém o padrão
    do detalhe para consistência (ou remove se não for usado na listagem).

### F2 — Backend: entidades e persistência (`Data.Entities`, `Data.MySql`, `Deployment.Database`)
- **Editar** `Data.Entities/Setup/Setup.cs`:
  - Remover `PriorizarTipoFrete`, `TipoFreteEnum`, `PriorizarTipoCliente`, `TipoClienteEnum`.
  - Adicionar `VolumeMinimoCarreta`, `VolumeMaximoCarreta`, `QuantidadeMinimaSkuPorLote`,
    `CapacidadeMaximaRecebimentoCliente`, `MixTipoFrete`.
- **Editar** `Data.Entities/Setup/SetupOrdemImportancia.cs`:
  - Adicionar `public string? Descricao { get; set; }`.
- **Editar** `Data.MySql/DbContext.cs`:
  - Remover configurações de `TipoFreteEnum`/`TipoClienteEnum`.
  - Adicionar configuração dos novos campos (nomes de coluna podem seguir PascalCase; o EF já
    mapeia por convenção para o mesmo nome).
- **Editar** `Deployment.Database/Scripts/Script010 - Setup.sql` conforme schema novo.

### F3 — Backend: serviço (`Service.SetupService/SetupService.cs`)
- **Editar** mapeamentos em `CriarAsync`, `AtualizarAsync`, `ClonarAsync`, `_MapDetalheAsync`.
- **Editar** `_ValidarModelo`:
  - Remover validações de `PriorizarTipoFrete`/`PriorizarTipoCliente`.
  - Validar `MixTipoFrete` entre 0 e 100 quando informado.
  - Validar `VolumeMinimoCarreta <= VolumeMaximoCarreta` quando ambos informados.
- **Editar** `_ValidarOrdemImportancia`:
  - Aceitar apenas os 5 critérios do novo enum.
- **Editar** `_PersistirOrdemImportancia`:
  - Se `Descricao` não vier preenchida, preencher com o texto padrão do critério.

### F4 — Backend: controller
- `SetupsController.cs` não precisa de mudanças diretas; apenas recompilar após mudanças nos modelos.

### F5 — Frontend: domínio (`domain/models/setup`, `domain/enums`)
- **Editar** `src/app/domain/enums/criterio-ordem.enum.ts`:
  - Trocar valores para espelhar o backend: `PriorizarFreteCIF`, `PiorizarClienteRevenda`,
    `Antecipar`, `AtenderDemanda`, `PedidoMaisAntigo`.
  - Adicionar `CRITERIO_ORDEM_DESCRICOES: Record<CriterioOrdemEnum, string>` com os textos padrão.
- **Remover** `src/app/domain/enums/tipo-frete.enum.ts` e `src/app/domain/enums/tipo-cliente.enum.ts`
  se não forem usados em outros módulos. Verificar uso em `cenarios/visualizar/semana` e
  `demanda` antes de remover. Se houver uso, manter os arquivos mas parar de exportar no setup.
- **Editar** `src/app/domain/models/setup/setup.model.ts`:
  - Remover campos de tipo frete/cliente e flags.
  - Adicionar `volumeMinimoCarreta`, `volumeMaximoCarreta`, `quantidadeMinimaSkuPorLote`,
    `capacidadeMaximaRecebimentoCliente`, `mixTipoFrete`.
  - Adicionar `descricao` em `SetupOrdemImportanciaRequest`/`Response`.

### F6 — Frontend: UI do setup
- **Editar** `src/app/modules/setups/setup.model.ts`:
  - Sincronizar interface `Setup` e labels.
- **Editar** `src/app/modules/setups/novo/novo-setup.component.ts`:
  - Remover imports de `TipoFreteEnum`/`TipoClienteEnum` e campos do form.
  - Adicionar campos novos com validadores (required para os 4 numéricos, range 0..100 para mix).
  - Atualizar `criteriosDisponiveis` para os 5 novos valores.
  - No `salvar()`, montar request com novos campos e descrição padrão dos critérios ativos.
- **Editar** `src/app/modules/setups/novo/novo-setup.component.html`:
  - Remover toggles "Priorizar tipo de frete" e "Priorizar tipo de cliente".
  - Adicionar inputs de volume mínimo/máximo da carreta, quantidade mínima SKU, capacidade máxima
    de recebimento.
  - Adicionar slider de mix de tipo de frete (CIF % à esquerda, FOB % à direita).
  - Na lista de ordem de importância, exibir a descrição de cada critério abaixo do label.
- **Editar** `src/app/modules/setups/visualizar/visualizar-setup.component.ts`/`.html`:
  - Remover lógica/labels de tipo frete/cliente.
  - Exibir os novos campos e as descrições dos critérios.
- **Editar** `src/app/modules/setups/setups.component.ts`:
  - Remover imports de `TIPO_FRETE_LABELS`/`TIPO_CLIENTE_LABELS` se não usados em outro lugar.

### F7 — Verificação
- `dotnet build arauco-otimizador-api.sln`
- `npx ng build --configuration development` no front
- (Opcional) executar testes existentes.

## Riscos / dúvidas a confirmar
1. As descrições dos critérios devem ser **editáveis** pelo usuário no formulário, ou apenas exibidas
   como texto explicativo fixo (persistido no banco)? Por ora implementamos como texto fixo
   preenchido pelo backend, exibido no front. Se quiser editável, basta adicionar um textarea
   por linha de critério e enviar no request.
2. Os campos `VolumeMinimoCarreta`/`VolumeMaximoCarreta` devem ser obrigatórios no formulário? O
   commit original de infraestrutura os tinha `required`. Mantemos obrigatórios por segurança
   operacional.
3. A remoção de `TipoFreteEnum`/`TipoClienteEnum` de `Common.Domain/Enums/Setup` não afeta o motor
   de otimização, que usa `TipoFreteEnum` do namespace `Common.Domain.Enums.Demanda`. Verificar
   se há outros usos do enum de `Setup` antes de apagar.
