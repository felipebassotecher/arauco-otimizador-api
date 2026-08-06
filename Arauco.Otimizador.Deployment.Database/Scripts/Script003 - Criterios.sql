-- Migra o esquema do modelo antigo (Parametro/ParametroValor/CenarioParametro) para o modelo de
-- critérios como regras pertencentes ao cenário (CenarioCriterio/CenarioArquivo) — spec §1/§3.9/§5.9.
-- Idempotente: usa DROP/CREATE TABLE IF [NOT] EXISTS para cobrir bancos que já rodaram o Script001
-- antigo (com as tabelas de Parametro) e bancos novos (que já sobem o esquema novo no Script001).

DROP TABLE IF EXISTS `CenarioParametro`;
DROP TABLE IF EXISTS `ParametroValor`;
DROP TABLE IF EXISTS `Parametro`;

CREATE TABLE IF NOT EXISTS `CenarioCriterio` (
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

CREATE TABLE IF NOT EXISTS `CenarioArquivo` (
	`CenarioId` CHAR(6) NOT NULL,
	`Nome` VARCHAR(200) NOT NULL,
	`Conteudo` LONGTEXT NOT NULL,
	`DataUpload` DATETIME NOT NULL,
	PRIMARY KEY (`CenarioId`),
	CONSTRAINT `FK_CenarioArquivo_Cenario` FOREIGN KEY (`CenarioId`) REFERENCES `Cenario` (`CenarioId`)
);