-- Master data consumida pelo motor de otimização (Dados/Carregador.cs): produtos, centros,
-- elegibilidade produto x centro, capacidade semanal e carteira real. Antes vinha de arquivos parquet
-- em Data/Datasets/ (mantidos no repo só como referência histórica, sem uso em runtime); agora é
-- lida diretamente destas tabelas via IUnitOfWork/DbContext.
-- Sem FK entre elas: são cargas de referência independentes, carregadas em lote.

CREATE TABLE `Centro` (
	`CentroId` INT NOT NULL,
	`Codigo` VARCHAR(50) NOT NULL,
	`Nome` VARCHAR(200) NOT NULL,
	`Ativo` BIT NOT NULL,
	`PorcentagemIndustria` INT NOT NULL,
	`PorcentagemRevenda` INT NOT NULL,
	PRIMARY KEY (`CentroId`)
);

CREATE TABLE `Produto` (
	`ProdutoId` VARCHAR(50) NOT NULL,
	`Descricao` VARCHAR(500) NULL,
	`LinhaProdutoId` INT NOT NULL,
	`LoteMinimoChapas` DECIMAL(18,4) NOT NULL,
	`LarguraMm` DECIMAL(18,4) NOT NULL,
	`ComprimentoMm` DECIMAL(18,4) NOT NULL,
	`EspessuraMm` DECIMAL(18,4) NOT NULL,
	`Ativo` BIT NOT NULL,
	PRIMARY KEY (`ProdutoId`)
);

CREATE TABLE `Elegibilidade` (
	`Id` INT NOT NULL AUTO_INCREMENT,
	`ProdutoId` VARCHAR(50) NOT NULL,
	`CentroId` INT NOT NULL,
	PRIMARY KEY (`Id`),
	KEY `IX_Elegibilidade_ProdutoId` (`ProdutoId`),
	KEY `IX_Elegibilidade_CentroId` (`CentroId`)
);

CREATE TABLE `Capacidade` (
	`Id` INT NOT NULL AUTO_INCREMENT,
	`CentroId` INT NOT NULL,
	`LinhaProducaoId` INT NOT NULL,
	`LinhaProdutoId` INT NOT NULL,
	`SemanaIso` INT NOT NULL,
	`Ano` INT NOT NULL,
	`Mes` INT NOT NULL,
	`TipoAlocacao` INT NOT NULL,
	`Quantidade` BIGINT NOT NULL,
	`DataCriacao` DATETIME NULL,
	PRIMARY KEY (`Id`),
	KEY `IX_Capacidade_CentroId` (`CentroId`),
	KEY `IX_Capacidade_LinhaProdutoId_Ano_SemanaIso` (`LinhaProdutoId`, `Ano`, `SemanaIso`)
);

-- Base de "carteira" real (master data), NÃO a tabela `Demanda` do cenário (que é o volume
-- digitado/importado por cenário via CSV).
CREATE TABLE `Carteira` (
	`CarteiraId` BIGINT NOT NULL,
	`ClienteId` VARCHAR(200) NOT NULL,
	`ClienteNome` VARCHAR(200) NULL,
	`ProdutoId` VARCHAR(50) NOT NULL,
	`LinhaProdutoId` INT NOT NULL,
	`VolumeM3` DECIMAL(18,3) NOT NULL,
	`DataDocumento` DATETIME NULL,
	`Incoterms` VARCHAR(20) NULL,
	`Segmento` VARCHAR(50) NULL,
	`CentroOriginal` BIGINT NOT NULL,
	PRIMARY KEY (`CarteiraId`),
	KEY `IX_Carteira_ClienteId` (`ClienteId`),
	KEY `IX_Carteira_ProdutoId` (`ProdutoId`)
);
