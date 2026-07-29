# Scripts de Banco de Dados - Arauco Hub API

Este diretório contém todos os scripts SQL necessários para criar e popular o banco de dados do sistema Arauco Hub API.

## Ordem de Execução

Execute os scripts na seguinte ordem:

### 1. `01_CreateTables.sql`
- Criação de todas as tabelas do sistema
- Definição de chaves primárias e estrangeiras
- Criação de índices para otimização de performance
- **Tabelas criadas:**
  - `Aplicacao` - Aplicações do sistema
  - `Sociedade` - Sociedades da empresa
  - `Empresa` - Empresas e filiais
  - `Filial` - Filiais
  - `CentroCusto` - Centros de custo
  - `PostoTrabalho` - Postos de trabalho
  - `Cargo` - Cargos dos colaboradores
  - `SituacaoColaborador` - Situações dos colaboradores
  - `Colaborador` - Dados dos colaboradores
  - `Perfil` - Perfis de usuário
  - `Regra` - Regras de acesso
  - `PerfilRegra` - Relacionamento entre perfis e regras
  - `Usuario` - Usuários do sistema
  - `TipoDocumento` - Tipos de documento
  - `Documento` - Documentos do sistema
  - `AssinaturaDocumento` - Assinaturas de documentos
  - `Cartao` - Cartões de reconhecimento

### 2. `02_InitialData.sql`
- Inserção de dados básicos necessários para o funcionamento do sistema
- **Dados inseridos:**
  - Aplicação Hub
  - Regras básicas de acesso
  - Perfis de usuário (Administrador e Usuário Padrão)
  - Situações de colaborador
  - Tipos de documento
  - Cargos básicos
  - Postos de trabalho
  - Centros de custo
  - Sociedades e empresas

### 3. `03_SampleData.sql` (Opcional)
- Dados de exemplo para desenvolvimento e teste
- **Dados de exemplo:**
  - Colaboradores de teste
  - Usuários de exemplo
  - Documentos de exemplo
  - Assinaturas de teste
  - Cartões de reconhecimento de exemplo

### 4. `04_CreateViews.sql`
- Criação de views úteis para consultas
- **Views criadas:**
  - `vw_ColaboradoresAtivos` - Colaboradores ativos com informações completas
  - `vw_UsuariosAtivos` - Usuários ativos com perfil e empresa
  - `vw_DocumentosPublicados` - Documentos publicados com estatísticas
  - `vw_CartoesDetalhados` - Cartões com informações detalhadas
  - `vw_EstatisticasAssinaturas` - Estatísticas de assinaturas por documento
  - `vw_RegrasPerPerfil` - Regras associadas a cada perfil

### 5. `05_StoredProcedures.sql`
- Criação de procedures úteis para operações do sistema
- **Procedures criadas:**
  - `sp_CriarUsuarioCompleto` - Criar usuário e colaborador em uma transação
  - `sp_DesativarUsuario` - Desativar usuário e colaborador
  - `sp_EstatisticasCartoesPorPeriodo` - Estatísticas de cartões por período
  - `sp_DocumentosPendentesAssinatura` - Documentos pendentes por colaborador
  - `sp_RegistrarAssinaturaDocumento` - Registrar assinatura de documento
  - `sp_RankingCartoesPorColaborador` - Ranking de cartões por colaborador

## Relacionamentos Principais

### Estrutura Organizacional
- `Sociedade` → `Empresa` (1:N)
- `Empresa` → `Colaborador` (1:N)
- `CentroCusto` → `Colaborador` (1:N)
- `Cargo` → `Colaborador` (1:N)
- `PostoTrabalho` → `Colaborador` (1:N)
- `SituacaoColaborador` → `Colaborador` (1:N)
- `Colaborador` → `Colaborador` (1:N - Gestor)

### Sistema de Usuários
- `Aplicacao` → `Usuario` (1:N)
- `Colaborador` → `Usuario` (1:N)
- `Perfil` → `Usuario` (1:N)
- `Perfil` ↔ `Regra` (N:N através de `PerfilRegra`)

### Sistema de Documentos
- `TipoDocumento` → `Documento` (1:N)
- `Usuario` → `Documento` (1:N - Publicação)
- `Documento` → `Documento` (1:N - Versões)
- `Documento` ↔ `Colaborador` (N:N através de `AssinaturaDocumento`)

### Sistema de Cartões
- `Cartao` referencia `Usuario` (Remetente e Destinatário)

## Enumerações

### AplicacaoEnum
- `1` = Hub

### TipoColaboradorEnum
- `1` = Empregado
- `2` = Terceiro

### CartaoTipoEnum
- `1` = Segurança
- `2` = Excelência e Inovação
- `3` = Trabalho em Equipe
- `4` = Bom Cidadão
- `5` = Compromisso

### RegraEnum
- `1` = Acesso Admin

## Índices Criados

Para otimizar a performance das consultas, foram criados os seguintes índices:

- `IX_Colaborador_Email` - Busca por email comercial
- `IX_Colaborador_Matricula` - Busca por matrícula
- `IX_Colaborador_Cpf` - Busca por CPF
- `IX_Usuario_Email` - Busca por email do usuário
- `IX_Usuario_NomeUsuario` - Busca por nome de usuário
- `IX_Cartao_DataHoraCriacao` - Ordenação de cartões por data
- `IX_Documento_DataHoraCriacao` - Ordenação de documentos por data
- `IX_AssinaturaDocumento_DataHoraAssinatura` - Busca por assinaturas por data

## Observações

1. **Campos Obrigatórios**: Verifique se todos os campos marcados como `NOT NULL` estão sendo preenchidos pela aplicação.

2. **Chaves Estrangeiras**: O sistema possui várias referências entre tabelas. Certifique-se de que os dados sejam inseridos na ordem correta.

3. **Campos de Controle**: Todas as entidades principais possuem campos de controle como `Ativo`, `Excluido`, `DataHoraRemocao`, etc.

4. **Suporte a Versionamento**: A tabela `Documento` suporta versionamento através do campo `DocumentoId_Anterior`.

5. **Auditoria**: Campos como `DataHoraCriacao`, `DataHoraAssinatura`, `EnderecoIp` permitem auditoria das operações.

6. **Flexibilidade**: Muitos campos são opcionais (`NULL`) para permitir flexibilidade na entrada de dados.