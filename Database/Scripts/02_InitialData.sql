-- ============================================
-- Arauco Hub API - Initial Data Script
-- Inserção de dados iniciais necessários
-- ============================================

-- Inserir dados na tabela Aplicacao
INSERT INTO Aplicacao (AplicacaoEnum, Nome) VALUES 
(1, 'Hub');

-- Inserir dados na tabela Regra
INSERT INTO Regra (RegraEnum, Sistema, Categoria, Nome) VALUES 
(1, 'Hub', 'Acessos', 'Acesso Admin');

-- Inserir perfil de administrador do sistema
INSERT INTO Perfil (Nome, Excluido, Sistema) VALUES 
('Administrador', 0, 1);

-- Associar regra de admin ao perfil administrador
INSERT INTO PerfilRegra (PerfilId, RegraEnum) VALUES 
(1, 1);

-- Inserir situações de colaborador básicas
INSERT INTO SituacaoColaborador (Codigo, Nome) VALUES 
('ATIVO', 'Ativo'),
('INATIVO', 'Inativo'),
('FERIAS', 'Férias'),
('LICENCA', 'Licença'),
('DEMITIDO', 'Demitido');

-- Inserir tipos de documento básicos
INSERT INTO TipoDocumento (TipoDocumentoId, Nome, Ativo, Excluido) VALUES 
('TERMO_USO', 'Termo de Uso', 1, 0),
('POLITICA_PRIVACIDADE', 'Política de Privacidade', 1, 0),
('CODIGO_CONDUTA', 'Código de Conduta', 1, 0),
('MANUAL_USUARIO', 'Manual do Usuário', 1, 0);

-- Inserir cargos básicos
INSERT INTO Cargo (Nome) VALUES 
('Analista'),
('Coordenador'),
('Gerente'),
('Diretor'),
('Supervisor'),
('Técnico'),
('Assistente'),
('Operador');

-- Inserir postos de trabalho básicos
INSERT INTO PostoTrabalho (Nome) VALUES 
('Escritório'),
('Fábrica'),
('Campo'),
('Floresta'),
('Laboratório'),
('Depósito'),
('Portaria'),
('Refeitório');

-- Inserir centros de custo básicos
INSERT INTO CentroCusto (Nome, Codigo, Ativo, Excluido) VALUES 
('Administração', 'ADM001', 1, 0),
('Recursos Humanos', 'RH001', 1, 0),
('Tecnologia da Informação', 'TI001', 1, 0),
('Operações', 'OPE001', 1, 0),
('Financeiro', 'FIN001', 1, 0),
('Comercial', 'COM001', 1, 0),
('Produção', 'PRD001', 1, 0),
('Qualidade', 'QLD001', 1, 0);

-- Inserir sociedades básicas
INSERT INTO Sociedade (Nome, Excluida) VALUES 
('Arauco do Brasil S.A.', 0),
('Arauco Florestal Arapoti S.A.', 0);

-- Inserir empresas básicas
INSERT INTO Empresa (Codigo, Nome, Cnpj, Ativa, Excluida, Terceiro, SociedadeId) VALUES 
('ARB001', 'Arauco do Brasil S.A.', '00.000.000/0001-00', 1, 0, 0, 1),
('AFA001', 'Arauco Florestal Arapoti S.A.', '00.000.000/0002-00', 1, 0, 0, 2);

-- Inserir filiais básicas
INSERT INTO Filial (Nome) VALUES 
('Matriz'),
('Filial Curitiba'),
('Filial Arapoti'),
('Filial Jaguariaíva');