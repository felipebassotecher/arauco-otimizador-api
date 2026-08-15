-- Reestrutura os campos operacionais do módulo Setup: remove a priorização por tipo de frete/cliente
-- e traz de volta os campos de volume/capacidade de carreta, quantidade mínima de SKU por lote e o
-- mix de tipo de frete. Também adiciona a descrição de cada critério em SetupOrdemImportancia.

ALTER TABLE `Setup`
	DROP COLUMN `PriorizarTipoFrete`,
	DROP COLUMN `TipoFreteId`,
	DROP COLUMN `PriorizarTipoCliente`,
	DROP COLUMN `TipoClienteId`,
	ADD COLUMN `VolumeMinimoCarreta` DECIMAL(18, 4) NULL,
	ADD COLUMN `VolumeMaximoCarreta` DECIMAL(18, 4) NULL,
	ADD COLUMN `QuantidadeMinimaSkuPorLote` INT NULL,
	ADD COLUMN `CapacidadeMaximaRecebimentoCliente` INT NULL,
	ADD COLUMN `MixTipoFrete` INT NULL;

ALTER TABLE `SetupOrdemImportancia`
	ADD COLUMN `Descricao` VARCHAR(500) NULL;
