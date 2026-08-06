CREATE TABLE `CenarioOtimizacaoResultado` (
	`ResultadoId` CHAR(6) NOT NULL,
	`CenarioId` CHAR(6) NOT NULL,
	`StatusSolver` VARCHAR(50) NOT NULL,
	`Segundos` DOUBLE NOT NULL,
	`Objetivo` DOUBLE NOT NULL,
	`Variaveis` INT NOT NULL,
	`Binarias` INT NOT NULL,
	`GreedyInicialM3` DECIMAL(18,2) NOT NULL,
	`GeradoEm` DATETIME NOT NULL,
	`FatorCapacidade` DECIMAL(9,4) NOT NULL,
	`CapacidadeTotal` BIGINT NOT NULL,
	`DemandaTotalM3` DECIMAL(18,2) NOT NULL,
	`DemandaElegivelM3` DECIMAL(18,2) NOT NULL,
	`ExcluidoPreflightM3` DECIMAL(18,2) NOT NULL,
	`AlocadoM3` DECIMAL(18,2) NOT NULL,
	`NaoAlocadoM3` DECIMAL(18,2) NOT NULL,
	`PercentualAlocado` DECIMAL(9,4) NOT NULL,
	`Itens` INT NOT NULL,
	`ItensExcluidos` INT NOT NULL,
	PRIMARY KEY (`ResultadoId`),
	KEY `IX_CenarioOtimizacaoResultado_CenarioId` (`CenarioId`),
	CONSTRAINT `FK_CenarioOtimizacaoResultado_Cenario` FOREIGN KEY (`CenarioId`) REFERENCES `Cenario` (`CenarioId`)
);

CREATE TABLE `OtimizacaoAlocacao` (
	`AlocacaoId` CHAR(6) NOT NULL,
	`ResultadoId` CHAR(6) NOT NULL,
	`Cliente` VARCHAR(200) NOT NULL,
	`Produto` VARCHAR(200) NOT NULL,
	`LinhaProdutoId` INT NOT NULL,
	`CentroId` INT NOT NULL,
	`Centro` VARCHAR(200) NOT NULL,
	`Ano` INT NOT NULL,
	`Semana` INT NOT NULL,
	`VolumeM3` DECIMAL(18,2) NOT NULL,
	`Cif` BIT NOT NULL,
	`Prioridade` BIGINT NOT NULL,
	`MotivoSemana` VARCHAR(500) NULL,
	`MotivoPlanta` VARCHAR(500) NULL,
	`FolgaAntesM3` DECIMAL(18,2) NOT NULL,
	`PlantasElegiveis` INT NOT NULL,
	`PosicaoPrioridade` INT NOT NULL,
	PRIMARY KEY (`AlocacaoId`),
	KEY `IX_OtimizacaoAlocacao_ResultadoId` (`ResultadoId`),
	CONSTRAINT `FK_OtimizacaoAlocacao_Resultado` FOREIGN KEY (`ResultadoId`) REFERENCES `CenarioOtimizacaoResultado` (`ResultadoId`)
);

CREATE TABLE `OtimizacaoNaoAlocado` (
	`NaoAlocadoId` CHAR(6) NOT NULL,
	`ResultadoId` CHAR(6) NOT NULL,
	`Cliente` VARCHAR(200) NOT NULL,
	`Produto` VARCHAR(200) NOT NULL,
	`LinhaProdutoId` INT NOT NULL,
	`VolumeM3` DECIMAL(18,2) NOT NULL,
	`DemandaM3` DECIMAL(18,2) NOT NULL,
	`Prioridade` BIGINT NOT NULL,
	`Motivo` VARCHAR(500) NULL,
	`MaiorFolgaM3` DECIMAL(18,2) NOT NULL,
	`PisoM3` DECIMAL(18,2) NOT NULL,
	PRIMARY KEY (`NaoAlocadoId`),
	KEY `IX_OtimizacaoNaoAlocado_ResultadoId` (`ResultadoId`),
	CONSTRAINT `FK_OtimizacaoNaoAlocado_Resultado` FOREIGN KEY (`ResultadoId`) REFERENCES `CenarioOtimizacaoResultado` (`ResultadoId`)
);

CREATE TABLE `OtimizacaoEmbarque` (
	`EmbarqueId` CHAR(6) NOT NULL,
	`ResultadoId` CHAR(6) NOT NULL,
	`Cliente` VARCHAR(200) NOT NULL,
	`CentroId` INT NOT NULL,
	`Centro` VARCHAR(200) NOT NULL,
	`Ano` INT NOT NULL,
	`Semana` INT NOT NULL,
	`Carretas` INT NOT NULL,
	`VolumeM3` DECIMAL(18,2) NOT NULL,
	`OcupacaoMedia` DECIMAL(9,4) NOT NULL,
	PRIMARY KEY (`EmbarqueId`),
	KEY `IX_OtimizacaoEmbarque_ResultadoId` (`ResultadoId`),
	CONSTRAINT `FK_OtimizacaoEmbarque_Resultado` FOREIGN KEY (`ResultadoId`) REFERENCES `CenarioOtimizacaoResultado` (`ResultadoId`)
);

CREATE TABLE `OtimizacaoOcupacao` (
	`OcupacaoId` CHAR(6) NOT NULL,
	`ResultadoId` CHAR(6) NOT NULL,
	`CentroId` INT NOT NULL,
	`Centro` VARCHAR(200) NOT NULL,
	`Ano` INT NOT NULL,
	`Semana` INT NOT NULL,
	`AlocadoM3` DECIMAL(18,2) NOT NULL,
	`CapacidadeM3` DECIMAL(18,2) NOT NULL,
	PRIMARY KEY (`OcupacaoId`),
	KEY `IX_OtimizacaoOcupacao_ResultadoId` (`ResultadoId`),
	CONSTRAINT `FK_OtimizacaoOcupacao_Resultado` FOREIGN KEY (`ResultadoId`) REFERENCES `CenarioOtimizacaoResultado` (`ResultadoId`)
);

CREATE TABLE `OtimizacaoCriterio` (
	`CriterioId` CHAR(6) NOT NULL,
	`ResultadoId` CHAR(6) NOT NULL,
	`Nome` VARCHAR(100) NOT NULL,
	`Descricao` VARCHAR(500) NULL,
	`Ordem` INT NOT NULL,
	`Peso` BIGINT NOT NULL,
	`Valor` DECIMAL(18,2) NOT NULL,
	PRIMARY KEY (`CriterioId`),
	KEY `IX_OtimizacaoCriterio_ResultadoId` (`ResultadoId`),
	CONSTRAINT `FK_OtimizacaoCriterio_Resultado` FOREIGN KEY (`ResultadoId`) REFERENCES `CenarioOtimizacaoResultado` (`ResultadoId`)
);
