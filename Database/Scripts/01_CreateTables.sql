-- Tabela de Aplicações
CREATE TABLE Aplicacao (
    AplicacaoEnum INT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL
);

-- Tabela de Sociedades
CREATE TABLE Sociedade (
    SociedadeId INT PRIMARY KEY,
    Nome VARCHAR(200) NOT NULL,
    Excluida BIT NOT NULL DEFAULT 0
);

-- Tabela de Empresas
CREATE TABLE Empresa (
    EmpresaId INT PRIMARY KEY,
    Codigo VARCHAR(50) NULL,
    Nome VARCHAR(200) NOT NULL,
    Cnpj VARCHAR(18) NULL,
    Estado VARCHAR(2) NULL,
    Cidade VARCHAR(100) NULL,
    Endereco VARCHAR(200) NULL,
    Numero VARCHAR(20) NULL,
    Complemento VARCHAR(50) NULL,
    Bairro VARCHAR(100) NULL,
    Cep VARCHAR(10) NULL,
    Ativa BIT NOT NULL DEFAULT 1,
    Excluida BIT NOT NULL DEFAULT 0,
    PoolVeiculos BIT NULL,
    Terceiro BIT NOT NULL DEFAULT 0,
    SociedadeId INT NULL,
    
    CONSTRAINT FK_Empresa_Sociedade FOREIGN KEY (SociedadeId) REFERENCES Sociedade(SociedadeId)
);

-- Tabela de Filiais
CREATE TABLE Filial (
    FilialId INT PRIMARY KEY,
    Nome VARCHAR(200) NOT NULL
);

-- Tabela de Centro de Custo
CREATE TABLE CentroCusto (
    CentroCustoId INT PRIMARY KEY,
    Nome VARCHAR(200) NOT NULL,
    Codigo VARCHAR(50) NOT NULL,
    Ativo BIT NOT NULL DEFAULT 1,
    Excluido BIT NOT NULL DEFAULT 0
);

-- Tabela de Posto de Trabalho
CREATE TABLE PostoTrabalho (
    PostoTrabalhoId INT PRIMARY KEY,
    Nome VARCHAR(200) NOT NULL
);

-- Tabela de Cargos
CREATE TABLE Cargo (
    CargoId INT PRIMARY KEY,
    Nome VARCHAR(200) NOT NULL
);

-- Tabela de Situação do Colaborador
CREATE TABLE SituacaoColaborador (
    SituacaoColaboradorId INT PRIMARY KEY,
    Codigo VARCHAR(20) NOT NULL,
    Nome VARCHAR(100) NOT NULL
);

-- Tabela de Colaboradores
CREATE TABLE Colaborador (
    ColaboradorId INT PRIMARY KEY,
    Nome VARCHAR(200) NOT NULL,
    EmailComercial VARCHAR(200) NULL,
    DataNascimento DATE NULL,
    Matricula VARCHAR(50) NULL,
    Ativo BIT NOT NULL DEFAULT 1,
    Excluido BIT NOT NULL DEFAULT 0,
    Celular VARCHAR(20) NULL,
    Telefone1 VARCHAR(20) NULL,
    Telefone2 VARCHAR(20) NULL,
    Genero VARCHAR(1) NULL,
    Cpf VARCHAR(14) NULL,
    EmailParticular VARCHAR(200) NULL,
    NumeroCracha VARCHAR(50) NULL,
    Ferias BIT NULL,
    TipoColaboradorEnum INT NOT NULL DEFAULT 1,
    EmailPortal VARCHAR(200) NULL,
    NomePortal VARCHAR(200) NULL,
    TelefonePortal VARCHAR(20) NULL,
    Cognito BIT NOT NULL DEFAULT 0,
    PodeAbrirSinistro BIT NOT NULL DEFAULT 0,
    UltimoAcessoPortal DATETIME2 NULL,
    UltimoAcessoAdmin DATETIME2 NULL,
    DataHoraCadastro DATETIME2 NULL,
    ColaboradorId_Gestor INT NULL,
    CentroCustoId INT NULL,
    EmpresaId INT NULL,
    CargoId INT NULL,
    PostoTrabalhoId INT NULL,
    SituacaoColaboradorId INT NULL,
    
    CONSTRAINT FK_Colaborador_Gestor FOREIGN KEY (ColaboradorId_Gestor) REFERENCES Colaborador(ColaboradorId),
    CONSTRAINT FK_Colaborador_CentroCusto FOREIGN KEY (CentroCustoId) REFERENCES CentroCusto(CentroCustoId),
    CONSTRAINT FK_Colaborador_Empresa FOREIGN KEY (EmpresaId) REFERENCES Empresa(EmpresaId),
    CONSTRAINT FK_Colaborador_Cargo FOREIGN KEY (CargoId) REFERENCES Cargo(CargoId),
    CONSTRAINT FK_Colaborador_PostoTrabalho FOREIGN KEY (PostoTrabalhoId) REFERENCES PostoTrabalho(PostoTrabalhoId),
    CONSTRAINT FK_Colaborador_SituacaoColaborador FOREIGN KEY (SituacaoColaboradorId) REFERENCES SituacaoColaborador(SituacaoColaboradorId)
);

-- Tabela de Perfis
CREATE TABLE Perfil (
    PerfilId INT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Excluido BIT NOT NULL DEFAULT 0,
    Sistema BIT NOT NULL DEFAULT 0
);

-- Tabela de Regras
CREATE TABLE Regra (
    RegraEnum INT PRIMARY KEY,
    Sistema VARCHAR(100) NOT NULL,
    Categoria VARCHAR(100) NOT NULL,
    Nome VARCHAR(200) NOT NULL
);

-- Tabela de relacionamento entre Perfil e Regras (Many-to-Many)
CREATE TABLE PerfilRegra (
    PerfilId INT NOT NULL,
    RegraEnum INT NOT NULL,
    
    PRIMARY KEY (PerfilId, RegraEnum),
    CONSTRAINT FK_PerfilRegra_Perfil FOREIGN KEY (PerfilId) REFERENCES Perfil(PerfilId),
    CONSTRAINT FK_PerfilRegra_Regra FOREIGN KEY (RegraEnum) REFERENCES Regra(RegraEnum)
);

-- Tabela de Usuários
CREATE TABLE Usuario (
    UsuarioId VARCHAR(100) PRIMARY KEY,
    Email VARCHAR(200) NOT NULL,
    Nome VARCHAR(200) NOT NULL,
    NomeUsuario VARCHAR(100) NOT NULL,
    Ativo BIT NOT NULL DEFAULT 1,
    DataHoraRemocao DATETIME2 NULL,
    AplicacaoEnum INT NOT NULL,
    ColaboradorId INT NOT NULL,
    PerfilId INT NOT NULL,
    
    CONSTRAINT FK_Usuario_Aplicacao FOREIGN KEY (AplicacaoEnum) REFERENCES Aplicacao(AplicacaoEnum),
    CONSTRAINT FK_Usuario_Colaborador FOREIGN KEY (ColaboradorId) REFERENCES Colaborador(ColaboradorId),
    CONSTRAINT FK_Usuario_Perfil FOREIGN KEY (PerfilId) REFERENCES Perfil(PerfilId)
);

-- Tabela de Cartões
CREATE TABLE Cartao (
    Id VARCHAR(100) PRIMARY KEY,
    TipoEnum INT NOT NULL,
    DataHoraCriacao DATETIME2 NOT NULL,
    Mensagem NVARCHAR(MAX) NOT NULL,
    Remetente INT NOT NULL,
    RemetenteId VARCHAR(100) NOT NULL,
    Destinatario INT NOT NULL,
    DestinatarioId VARCHAR(100) NOT NULL
);
