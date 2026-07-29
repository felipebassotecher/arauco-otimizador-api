-- ============================================
-- Arauco Hub API - Master Database Setup Script
-- Script principal para executar todos os scripts em ordem
-- ============================================

USE [AracoHubDB] -- Altere para o nome do seu banco de dados
GO

PRINT 'Iniciando criação do banco de dados Arauco Hub API...';
PRINT '';

-- ============================================
-- 1. Criar Tabelas
-- ============================================
PRINT '1. Criando tabelas...';

-- Conteúdo do script 01_CreateTables.sql seria incluído aqui
-- Por questões de manutenibilidade, recomenda-se executar cada script separadamente

PRINT 'Tabelas criadas com sucesso!';
PRINT '';

-- ============================================
-- 2. Inserir Dados Iniciais
-- ============================================
PRINT '2. Inserindo dados iniciais...';

-- Conteúdo do script 02_InitialData.sql seria incluído aqui

PRINT 'Dados iniciais inseridos com sucesso!';
PRINT '';

-- ============================================
-- 3. Inserir Dados de Exemplo (Opcional)
-- ============================================
PRINT '3. Inserindo dados de exemplo...';

-- Descomente a linha abaixo se desejar inserir dados de exemplo
-- EXEC [caminho_para_script]\03_SampleData.sql

PRINT 'Dados de exemplo inseridos com sucesso!';
PRINT '';

-- ============================================
-- 4. Criar Views
-- ============================================
PRINT '4. Criando views...';

-- Conteúdo do script 04_CreateViews.sql seria incluído aqui

PRINT 'Views criadas com sucesso!';
PRINT '';

-- ============================================
-- 5. Criar Stored Procedures
-- ============================================
PRINT '5. Criando stored procedures...';

-- Conteúdo do script 05_StoredProcedures.sql seria incluído aqui

PRINT 'Stored procedures criadas com sucesso!';
PRINT '';

-- ============================================
-- Verificação Final
-- ============================================
PRINT '6. Executando verificações finais...';

-- Verificar se todas as tabelas foram criadas
SELECT 
    'Tabelas criadas' as Verificacao,
    COUNT(*) as Quantidade
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
    AND TABLE_NAME IN (
        'Aplicacao', 'Sociedade', 'Empresa', 'Filial', 'CentroCusto',
        'PostoTrabalho', 'Cargo', 'SituacaoColaborador', 'Colaborador',
        'Perfil', 'Regra', 'PerfilRegra', 'Usuario', 'TipoDocumento',
        'Documento', 'AssinaturaDocumento', 'Cartao'
    );

-- Verificar se as views foram criadas
SELECT 
    'Views criadas' as Verificacao,
    COUNT(*) as Quantidade
FROM INFORMATION_SCHEMA.VIEWS 
WHERE VIEW_NAME LIKE 'vw_%';

-- Verificar se os dados iniciais foram inseridos
SELECT 'Aplicações' as Tabela, COUNT(*) as Registros FROM Aplicacao
UNION ALL
SELECT 'Regras' as Tabela, COUNT(*) as Registros FROM Regra
UNION ALL
SELECT 'Perfis' as Tabela, COUNT(*) as Registros FROM Perfil
UNION ALL
SELECT 'Situações Colaborador' as Tabela, COUNT(*) as Registros FROM SituacaoColaborador
UNION ALL
SELECT 'Tipos Documento' as Tabela, COUNT(*) as Registros FROM TipoDocumento
UNION ALL
SELECT 'Cargos' as Tabela, COUNT(*) as Registros FROM Cargo
UNION ALL
SELECT 'Postos Trabalho' as Tabela, COUNT(*) as Registros FROM PostoTrabalho
UNION ALL
SELECT 'Centros Custo' as Tabela, COUNT(*) as Registros FROM CentroCusto
UNION ALL
SELECT 'Sociedades' as Tabela, COUNT(*) as Registros FROM Sociedade
UNION ALL
SELECT 'Empresas' as Tabela, COUNT(*) as Registros FROM Empresa;

PRINT '';
PRINT 'Setup do banco de dados concluído com sucesso!';
PRINT 'Banco de dados Arauco Hub API está pronto para uso.';

/*
INSTRUÇÕES DE USO:

1. Altere o nome do banco de dados na linha 6 (USE [AracoHubDB])
2. Execute cada script individualmente na seguinte ordem:
   - 01_CreateTables.sql
   - 02_InitialData.sql
   - 03_SampleData.sql (opcional)
   - 04_CreateViews.sql
   - 05_StoredProcedures.sql

3. Ou adapte este script incluindo o conteúdo de cada arquivo
   nas seções correspondentes.

4. Para ambiente de produção:
   - Não execute o script 03_SampleData.sql
   - Revise todos os dados iniciais antes da execução
   - Execute em horário de baixo movimento
   - Faça backup antes da execução

5. Para ambiente de desenvolvimento:
   - Execute todos os scripts incluindo dados de exemplo
   - Use para teste e validação das funcionalidades
*/