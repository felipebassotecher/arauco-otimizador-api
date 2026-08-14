-- Segmento (INDUSTRIA/REVENDA) na Demanda, usado pelo critério personalizado "Tipo de Cliente" do V2.
-- Default 2 = Revenda, mesmo default que o parser de CSV já assume quando a coluna não vem informada.
ALTER TABLE `Demanda` ADD COLUMN `SegmentoId` INT NOT NULL DEFAULT 2;

CREATE TABLE `CenarioOtimizacaoV2Resultado` (
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
	KEY `IX_CenarioOtimizacaoV2Resultado_CenarioId` (`CenarioId`),
	CONSTRAINT `FK_CenarioOtimizacaoV2Resultado_Cenario` FOREIGN KEY (`CenarioId`) REFERENCES `Cenario` (`CenarioId`)
);

-- Pedido gerado pelo motor V2 (cliente + produto + centro + semana, granularidade de decisão do
-- CP-SAT). Pinado=true fixa a alocação: numa nova execução ela não é tocada, e seu volume é descontado
-- da demanda/capacidade antes de reotimizar o restante (ver OtimizadorV2Service.DescontarPinados).
CREATE TABLE `PedidoV2` (
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
	KEY `IX_PedidoV2_CenarioId_Ano_Semana` (`CenarioId`, `Ano`, `Semana`),
	CONSTRAINT `FK_PedidoV2_Cenario` FOREIGN KEY (`CenarioId`) REFERENCES `Cenario` (`CenarioId`),
	CONSTRAINT `FK_PedidoV2_Resultado` FOREIGN KEY (`ResultadoId`) REFERENCES `CenarioOtimizacaoV2Resultado` (`ResultadoId`)
);

CREATE TABLE `PedidoV2NaoAlocado` (
	`NaoAlocadoId` CHAR(6) NOT NULL,
	`ResultadoId` CHAR(6) NOT NULL,
	`Cliente` VARCHAR(200) NOT NULL,
	`Material` VARCHAR(200) NOT NULL,
	`LinhaProdutoId` INT NOT NULL,
	`VolumeM3` DECIMAL(18,2) NOT NULL,
	`Motivo` VARCHAR(500) NULL,
	PRIMARY KEY (`NaoAlocadoId`),
	KEY `IX_PedidoV2NaoAlocado_ResultadoId` (`ResultadoId`),
	CONSTRAINT `FK_PedidoV2NaoAlocado_Resultado` FOREIGN KEY (`ResultadoId`) REFERENCES `CenarioOtimizacaoV2Resultado` (`ResultadoId`)
);
