-- ============================================
-- Arauco Hub API - Sample Data Script
-- Dados de exemplo para desenvolvimento e teste
-- ============================================

-- Inserir colaborador de exemplo (necessário para criar usuários)
INSERT INTO Colaborador (
    Nome, 
    EmailComercial, 
    DataNascimento, 
    Matricula, 
    Ativo, 
    Excluido, 
    Celular, 
    Genero, 
    Cpf, 
    TipoColaboradorEnum, 
    EmailPortal, 
    NomePortal, 
    Cognito, 
    PodeAbrirSinistro, 
    DataHoraCadastro,
    EmpresaId,
    CargoId,
    CentroCustoId,
    PostoTrabalhoId,
    SituacaoColaboradorId
) VALUES 
(
    'Administrador do Sistema', 
    'admin@arauco.com', 
    '1980-01-01', 
    'ADM001', 
    1, 
    0, 
    '(41) 99999-9999', 
    'M', 
    '000.000.000-00', 
    1, 
    'admin@arauco.com', 
    'Admin', 
    1, 
    1, 
    GETDATE(),
    1,
    4,
    1,
    1,
    1
),
(
    'João Silva Santos', 
    'joao.silva@arauco.com', 
    '1985-06-15', 
    'USR001', 
    1, 
    0, 
    '(41) 98888-8888', 
    'M', 
    '111.111.111-11', 
    1, 
    'joao.silva@arauco.com', 
    'João Silva', 
    1, 
    0, 
    GETDATE(),
    1,
    1,
    2,
    1,
    1
),
(
    'Maria Oliveira Costa', 
    'maria.oliveira@arauco.com', 
    '1990-03-22', 
    'USR002', 
    1, 
    0, 
    '(41) 97777-7777', 
    'F', 
    '222.222.222-22', 
    1, 
    'maria.oliveira@arauco.com', 
    'Maria Oliveira', 
    1, 
    0, 
    GETDATE(),
    1,
    2,
    2,
    1,
    1
);

-- Inserir usuários de exemplo
INSERT INTO Usuario (
    UsuarioId, 
    Email, 
    Nome, 
    NomeUsuario, 
    Ativo, 
    AplicacaoEnum, 
    ColaboradorId, 
    PerfilId
) VALUES 
(
    'admin-001', 
    'admin@arauco.com', 
    'Administrador do Sistema', 
    'admin', 
    1, 
    1, 
    1, 
    1
),
(
    'user-001', 
    'joao.silva@arauco.com', 
    'João Silva Santos', 
    'joao.silva', 
    1, 
    1, 
    2, 
    2
),
(
    'user-002', 
    'maria.oliveira@arauco.com', 
    'Maria Oliveira Costa', 
    'maria.oliveira', 
    1, 
    1, 
    3, 
    2
);

-- Inserir documentos de exemplo
INSERT INTO Documento (
    DocumentoId, 
    DataHoraCriacao, 
    DataHoraPublicacao, 
    Versao, 
    StorageKey, 
    TipoDocumentoId, 
    UsuarioId_Publicacao
) VALUES 
(
    'DOC-001', 
    GETDATE(), 
    GETDATE(), 
    1, 
    'documents/termo-uso-v1.pdf', 
    'TERMO_USO', 
    'admin-001'
),
(
    'DOC-002', 
    GETDATE(), 
    GETDATE(), 
    1, 
    'documents/politica-privacidade-v1.pdf', 
    'POLITICA_PRIVACIDADE', 
    'admin-001'
);

-- Inserir assinaturas de exemplo
INSERT INTO AssinaturaDocumento (
    DocumentoId, 
    ColaboradorId, 
    DataHoraAssinatura, 
    EnderecoIp
) VALUES 
(
    'DOC-001', 
    2, 
    GETDATE(), 
    '192.168.1.100'
),
(
    'DOC-001', 
    3, 
    GETDATE(), 
    '192.168.1.101'
),
(
    'DOC-002', 
    2, 
    GETDATE(), 
    '192.168.1.100'
);

-- Inserir cartões de exemplo
INSERT INTO Cartao (
    Id, 
    TipoEnum, 
    DataHoraCriacao, 
    Mensagem, 
    Remetente, 
    RemetenteId, 
    Destinatario, 
    DestinatarioId
) VALUES 
(
    'CARD-001', 
    1, 
    GETDATE(), 
    'Parabéns pela excelente atitude de segurança demonstrada hoje!', 
    1, 
    'admin-001', 
    2, 
    'user-001'
),
(
    'CARD-002', 
    2, 
    GETDATE(), 
    'Sua iniciativa de melhoria do processo foi excelente!', 
    1, 
    'admin-001', 
    3, 
    'user-002'
),
(
    'CARD-003', 
    3, 
    GETDATE(), 
    'Obrigado pela colaboração no projeto da equipe!', 
    2, 
    'user-001', 
    3, 
    'user-002'
);