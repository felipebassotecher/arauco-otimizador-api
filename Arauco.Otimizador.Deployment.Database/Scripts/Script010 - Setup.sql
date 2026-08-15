-- Cria as tabelas do módulo Setup (configurações de otimização).
-- Todas as operações são feitas via UnitOfWork/DbContext; não há views nem stored procedures.

CREATE TABLE `Setup` (
	`SetupId` CHAR(6) NOT NULL,
	`Nome` VARCHAR(200) NOT NULL,
	`Descricao` VARCHAR(500) NULL,
	`PesoMinimoCarregamento` DECIMAL(18, 4) NULL,
	`PercentualVariacaoMediaVenda` INT NULL,
	`QuantidadeMaximaTrocas` INT NULL,
	`UtilizarToleranciaPeso` BIT NOT NULL DEFAULT 0,
	`PermitirCarregarAbaixoPesoMinimo` BIT NOT NULL DEFAULT 0,
	`PriorizarTipoFrete` BIT NOT NULL DEFAULT 0,
	`TipoFreteId` INT NULL,
	`PriorizarTipoCliente` BIT NOT NULL DEFAULT 0,
	`TipoClienteId` INT NULL,
	`DataCriacao` DATETIME NOT NULL,
	`DataAlteracao` DATETIME NULL,
	PRIMARY KEY (`SetupId`)
);

CREATE TABLE `SetupOrdemImportancia` (
	`Id` INT NOT NULL AUTO_INCREMENT,
	`SetupId` CHAR(6) NOT NULL,
	`CriterioOrdemId` INT NOT NULL,
	`Ordem` INT NOT NULL,
	PRIMARY KEY (`Id`),
	KEY `IX_SetupOrdemImportancia_SetupId` (`SetupId`),
	CONSTRAINT `FK_SetupOrdemImportancia_Setup` FOREIGN KEY (`SetupId`) REFERENCES `Setup` (`SetupId`) ON DELETE CASCADE
);
