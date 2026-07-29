CREATE TABLE `Cenario` (
	`CenarioId` CHAR(6) NOT NULL,
	`Nome` VARCHAR(200) NOT NULL,
	`ArquivoNome` VARCHAR(200) NOT NULL,
	`DataCriacao` DATETIME NOT NULL,
	`DataUltimoProcessamento` DATETIME NULL,
	`StatusCenarioId` INT NOT NULL,
	`Submetido` BIT NOT NULL,
	PRIMARY KEY (`CenarioId`)
);

CREATE TABLE `Parametro` (
	`ParametroId` CHAR(6) NOT NULL,
	`Nome` VARCHAR(200) NOT NULL,
	`Chave` VARCHAR(100) NOT NULL,
	`Descricao` VARCHAR(500) NOT NULL,
	`Peso` DOUBLE NOT NULL,
	`Ativo` BIT NOT NULL,
	PRIMARY KEY (`ParametroId`),
	UNIQUE KEY `UQ_Parametro_Chave` (`Chave`)
);

CREATE TABLE `ParametroValor` (
	`Id` INT NOT NULL AUTO_INCREMENT,
	`ParametroId` CHAR(6) NOT NULL,
	`Valor` VARCHAR(200) NOT NULL,
	`Rotulo` VARCHAR(200) NOT NULL,
	`Peso` DOUBLE NULL,
	PRIMARY KEY (`Id`),
	KEY `IX_ParametroValor_ParametroId` (`ParametroId`),
	CONSTRAINT `FK_ParametroValor_Parametro` FOREIGN KEY (`ParametroId`) REFERENCES `Parametro` (`ParametroId`)
);

CREATE TABLE `CenarioParametro` (
	`CenarioId` CHAR(6) NOT NULL,
	`ParametroId` CHAR(6) NOT NULL,
	PRIMARY KEY (`CenarioId`, `ParametroId`),
	CONSTRAINT `FK_CenarioParametro_Cenario` FOREIGN KEY (`CenarioId`) REFERENCES `Cenario` (`CenarioId`),
	CONSTRAINT `FK_CenarioParametro_Parametro` FOREIGN KEY (`ParametroId`) REFERENCES `Parametro` (`ParametroId`)
);

CREATE TABLE `Demanda` (
	`DemandaId` CHAR(6) NOT NULL,
	`CenarioId` CHAR(6) NOT NULL,
	`Cliente` VARCHAR(200) NOT NULL,
	`Material` VARCHAR(200) NOT NULL,
	`Volume` DECIMAL(18,3) NOT NULL,
	`DataEntregaDesejada` DATETIME NOT NULL,
	`TipoFreteId` INT NOT NULL,
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
