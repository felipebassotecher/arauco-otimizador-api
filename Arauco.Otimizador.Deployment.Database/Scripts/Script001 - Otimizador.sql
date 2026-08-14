CREATE TABLE `Cenario` (
	`CenarioId` CHAR(6) NOT NULL,
	`Nome` VARCHAR(200) NOT NULL,
	`ArquivoNome` VARCHAR(200) NULL,
	`DataCriacao` DATETIME NOT NULL,
	`DataUltimoProcessamento` DATETIME NULL,
	`StatusCenarioId` INT NOT NULL,
	`Submetido` BIT NOT NULL,
	PRIMARY KEY (`CenarioId`)
);

CREATE TABLE `CenarioCriterio` (
	`Id` INT NOT NULL AUTO_INCREMENT,
	`CenarioId` CHAR(6) NOT NULL,
	`CriterioChave` VARCHAR(100) NOT NULL,
	`OperadorId` INT NOT NULL,
	`Valor` VARCHAR(200) NOT NULL,
	`Peso` INT NOT NULL,
	PRIMARY KEY (`Id`),
	KEY `IX_CenarioCriterio_CenarioId` (`CenarioId`),
	CONSTRAINT `FK_CenarioCriterio_Cenario` FOREIGN KEY (`CenarioId`) REFERENCES `Cenario` (`CenarioId`)
);

CREATE TABLE `CenarioArquivo` (
	`CenarioId` CHAR(6) NOT NULL,
	`Nome` VARCHAR(200) NOT NULL,
	`Conteudo` LONGTEXT NOT NULL,
	`DataUpload` DATETIME NOT NULL,
	PRIMARY KEY (`CenarioId`),
	CONSTRAINT `FK_CenarioArquivo_Cenario` FOREIGN KEY (`CenarioId`) REFERENCES `Cenario` (`CenarioId`)
);

CREATE TABLE `Demanda` (
	`DemandaId` CHAR(6) NOT NULL,
	`CenarioId` CHAR(6) NOT NULL,
	`Cliente` VARCHAR(200) NOT NULL,
	`Material` VARCHAR(200) NOT NULL,
	`Volume` DECIMAL(18,3) NOT NULL,
	`DataEntregaDesejada` DATETIME NOT NULL,
	`TipoFreteId` INT NOT NULL,
	-- Segmento (INDUSTRIA/REVENDA), usado pelo critério personalizado "Tipo de Cliente" do motor de
	-- otimização. Default 2 = Revenda, mesmo default que o parser de CSV já assume quando a coluna não
	-- vem informada.
	`SegmentoId` INT NOT NULL DEFAULT 2,
	PRIMARY KEY (`DemandaId`),
	KEY `IX_Demanda_CenarioId` (`CenarioId`),
	CONSTRAINT `FK_Demanda_Cenario` FOREIGN KEY (`CenarioId`) REFERENCES `Cenario` (`CenarioId`)
);

CREATE TABLE `Pedido` (
	`PedidoId` CHAR(6) NOT NULL,
	`CenarioId` CHAR(6) NOT NULL,
	`Cliente` VARCHAR(200) NOT NULL,
	`TipoFreteId` INT NOT NULL,
	`Volume` DECIMAL(18,3) NOT NULL,
	`DataEntregaPrevista` DATETIME NOT NULL,
	`Ano` INT NOT NULL,
	`Semana` INT NOT NULL,
	`Pinado` BIT NOT NULL,
	`Grupo` VARCHAR(200) NULL,
	PRIMARY KEY (`PedidoId`),
	KEY `IX_Pedido_CenarioId_Ano_Semana` (`CenarioId`, `Ano`, `Semana`),
	CONSTRAINT `FK_Pedido_Cenario` FOREIGN KEY (`CenarioId`) REFERENCES `Cenario` (`CenarioId`)
);