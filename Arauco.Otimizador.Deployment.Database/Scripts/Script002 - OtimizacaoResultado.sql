CREATE TABLE `CenarioOtimizacaoResultado` (
	`ResultadoId` CHAR(6) NOT NULL,
	`CenarioId` CHAR(6) NOT NULL,
	`GeradoEm` DATETIME NOT NULL,
	`StatusSolver` VARCHAR(50) NOT NULL,
	`Segundos` DOUBLE NOT NULL,
	`Objetivo` DOUBLE NOT NULL,
	`Variaveis` INT NOT NULL,
	`Binarias` INT NOT NULL,
	`CapacidadeTotal` BIGINT NOT NULL,
	`DemandaTotalM3` DECIMAL(18,2) NOT NULL,
	`DemandaElegivelM3` DECIMAL(18,2) NOT NULL,
	`AlocadoM3` DECIMAL(18,2) NOT NULL,
	`NaoAlocadoM3` DECIMAL(18,2) NOT NULL,
	`Itens` INT NOT NULL,
	`ItensExcluidos` INT NOT NULL,
	PRIMARY KEY (`ResultadoId`),
	KEY `IX_CenarioOtimizacaoResultado_CenarioId` (`CenarioId`),
	CONSTRAINT `FK_CenarioOtimizacaoResultado_Cenario` FOREIGN KEY (`CenarioId`) REFERENCES `Cenario` (`CenarioId`)
);

-- Pedido gerado pelo motor de otimização (cliente + produto + centro + semana, granularidade de
-- decisão do CP-SAT). Pinado=true fixa a alocação: numa nova execução ela não é tocada, e seu volume
-- é descontado da demanda/capacidade antes de reotimizar o restante (ver OtimizadorService.DescontarPinados).
-- Nome distinto do `Pedido` já existente (fluxo simples de agrupamento cliente+semana, POST
-- /cenarios/{id}/processar) — são recursos diferentes, sem relação entre si.
CREATE TABLE `PedidoOtimizado` (
	`PedidoId` CHAR(6) NOT NULL,
	`CenarioId` CHAR(6) NOT NULL,
	`ResultadoId` CHAR(6) NOT NULL,
	`Cliente` VARCHAR(200) NOT NULL,
	`Material` VARCHAR(200) NOT NULL,
	`LinhaProdutoId` INT NOT NULL,
	`CentroId` INT NOT NULL,
	`Centro` VARCHAR(200) NOT NULL,
	`TipoFreteId` INT NOT NULL,
	`Volume` DECIMAL(18,2) NOT NULL,
	`Ano` INT NOT NULL,
	`Semana` INT NOT NULL,
	`Pinado` BIT NOT NULL,
	`ScorePeso` INT NOT NULL,
	PRIMARY KEY (`PedidoId`),
	KEY `IX_PedidoOtimizado_CenarioId_Ano_Semana` (`CenarioId`, `Ano`, `Semana`),
	CONSTRAINT `FK_PedidoOtimizado_Cenario` FOREIGN KEY (`CenarioId`) REFERENCES `Cenario` (`CenarioId`),
	CONSTRAINT `FK_PedidoOtimizado_Resultado` FOREIGN KEY (`ResultadoId`) REFERENCES `CenarioOtimizacaoResultado` (`ResultadoId`)
);

CREATE TABLE `PedidoOtimizadoNaoAlocado` (
	`NaoAlocadoId` CHAR(6) NOT NULL,
	`ResultadoId` CHAR(6) NOT NULL,
	`Cliente` VARCHAR(200) NOT NULL,
	`Material` VARCHAR(200) NOT NULL,
	`LinhaProdutoId` INT NOT NULL,
	`VolumeM3` DECIMAL(18,2) NOT NULL,
	`Motivo` VARCHAR(500) NULL,
	PRIMARY KEY (`NaoAlocadoId`),
	KEY `IX_PedidoOtimizadoNaoAlocado_ResultadoId` (`ResultadoId`),
	CONSTRAINT `FK_PedidoOtimizadoNaoAlocado_Resultado` FOREIGN KEY (`ResultadoId`) REFERENCES `CenarioOtimizacaoResultado` (`ResultadoId`)
);
