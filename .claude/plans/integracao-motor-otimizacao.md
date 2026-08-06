# Plano: Integrar o motor de otimização CP-SAT ao arauco-otimizador-api

## Decisões tomadas com você
- Endpoint: novo POST /cenarios/{id}/otimizar, mantendo o POST /cenarios/{id}/processar atual intacto.
- Bases de dados parquet: caminho padrão Data/Datasets/ dentro do repo, sobrescrito via appsettings.json (Otimizador:DatasetsPath).
- Projeto do motor: novo projeto Arauco.Otimizador.Service.OtimizadorService para isolar Google.OrTools e Parquet.Net.
- Persistência do resultado: novas entidades para o resultado otimizado — não altera a tabela Pedido existente.

## Contexto
O projeto de origem contém um motor CP-SAT completo que lê Parquet, monta horizonte ISO, calibra capacidade, faz pre-flight e resolve com critérios soft ranqueáveis. O projeto de destino possui tabelas Cenario/Demanda/Pedido e arquitetura Controller → Service → UnitOfWork → Repository → DbContext (MySQL).

## Objetivo desta fase
Disponibilizar a otimização de pedidos via WebApi, adaptada à arquitetura do novo projeto, sem substituir o processamento existente.

## Estrutura de arquivos a criar/alterar

### 1. Novo projeto de motor
- Arauco.Otimizador.Service.OtimizadorService/Arauco.Otimizador.Service.OtimizadorService.csproj
  - TargetFramework net9.0
  - Referências: Common.Domain, Data.Entities, Service.Base, Techer.Common.Id
  - Pacotes: Google.OrTools 9.15.6755 e Parquet.Net 6.0.3
- OtimizadorService.cs — implementa IOtimizadorService
- Config/Config.cs — configuração do motor
- Config/ModoCapacidade.cs — enum copiado do original
- Dados/TabelaParquet.cs, Dados/Carregador.cs, Dados/Modelos.cs, Dados/GeradorCarteira.cs
- Capacidade/ProvedorCapacidade.cs
- Modelo/Otimizacao.cs, Modelo/Greedy.cs, Modelo/Preparacao.cs, Modelo/Objetivo.cs, Modelo/Explicacao.cs, Modelo/Motivos.cs
- Mapeamento/DemandaParaCarteiraMapper.cs

### 2. Contratos em Common.Domain
- Services/Otimizador/IOtimizadorService.cs
- Models/Otimizador/OtimizacaoRequest.cs, OtimizacaoResponse.cs e modelos filhos

### 3. Entidades e banco de dados
- Data.Entities/Otimizador/CenarioOtimizacaoResultado.cs
- Data.Entities/Otimizador/OtimizacaoAlocacao.cs
- Data.Entities/Otimizador/OtimizacaoNaoAlocado.cs
- Data.Entities/Otimizador/OtimizacaoEmbarque.cs
- Data.Entities/Otimizador/OtimizacaoOcupacao.cs
- Data.Entities/Otimizador/OtimizacaoCriterio.cs
- Data.MySql/DbContext.cs — adicionar DbSets
- Data.Entities/IUnitOfWork.cs — adicionar repositórios
- Data.MySql/UnitOfWork.cs — lazy-init
- Deployment.Database/Scripts/Script004 - OtimizacaoResultado.sql
- Deployment.Database/csproj — adicionar EmbeddedResource

### 4. Controller e wiring
- WebApi/Controllers/CenariosController.cs — adicionar POST {id}/otimizar
- WebApi/Startup.cs — registrar IOtimizadorService
- WebApi/csproj — referenciar novo projeto
- sln — adicionar projeto na solution folder Service

### 5. Configuração dos datasets
- WebApi/appsettings.json — adicionar Otimizador:DatasetsPath = Data/Datasets
- OtimizadorService resolve caminho absoluto via AppContext.BaseDirectory

## Mapeamento de dados
Demanda → LinhaCarteira:
- ClienteId = Demanda.Cliente
- ProdutoId = Demanda.Material
- VolumeM3 = (double)Demanda.Volume
- DataDocumento = Demanda.DataEntregaDesejada
- Incoterms = Demanda.TipoFreteEnum.ToString()
- Segmento = REVENDA (default)
- CentroOriginal = primeira planta elegível do produto

Produtos/Elegibilidade/Capacidade vêm dos arquivos parquet em Data/Datasets/.

## Critérios e configuração
Inicialmente o motor roda com Config default do projeto original (horizonte 8, capacidade simulada, alvo 0.8, carreta ativa). A OtimizacaoRequest permite sobrescrever campos simples.

## Ajustes no motor original
1. Trocar namespaces internos para Arauco.Otimizador.Service.OtimizadorService.*
2. Substituir DateTime.Now por DateTime.UtcNow nas entidades
3. Manter unidades double m3 internamente; converter para decimal só nos DTOs de API
4. Reescrever Executor como método de instância, sem cache estático global

## Script MySQL resumido
Criar CenarioOtimizacaoResultado, OtimizacaoAlocacao, OtimizacaoNaoAlocado, OtimizacaoEmbarque, OtimizacaoOcupacao e OtimizacaoCriterio com FK para Resultado e para Cenario.

## Verificação
1. dotnet build arauco-otimizador-api.sln --configuration Debug → 0 erros
2. Aplicar Script004 no MySQL local
3. Rodar WebApi e chamar POST /cenarios/{id}/otimizar → retorna OtimizacaoResponse

## Não alterado
- ProcessarAsync existente continua agrupando demandas por cliente+semana ISO na tabela Pedido
- Tabela Pedido não é modificada
- Auth continua removida
