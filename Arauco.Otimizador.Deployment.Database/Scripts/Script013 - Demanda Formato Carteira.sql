-- Amplia a tabela Demanda para o formato "carteira em aberto" do ADC (mesma extração usada como
-- referência em otimizador-teste-entrega/sql/extracao/demanda.sql), substituindo o Segmento fechado
-- (Industria/Revenda) por texto livre — a extração real traz segmentos como ESPECIALISTA, MAYORISTA,
-- EXPORTAÇÃO etc., que o enum fechado não representava.

ALTER TABLE `Demanda`
	ADD COLUMN `CarteiraId` BIGINT NOT NULL DEFAULT 0,
	ADD COLUMN `ClienteNome` VARCHAR(200) NOT NULL DEFAULT '',
	ADD COLUMN `LinhaProdutoId` INT NOT NULL DEFAULT 0,
	ADD COLUMN `DataDocumento` DATETIME NULL,
	ADD COLUMN `CentroOriginal` INT NOT NULL DEFAULT 0,
	ADD COLUMN `Segmento` VARCHAR(100) NOT NULL DEFAULT '',
	DROP COLUMN `SegmentoId`;

-- Sem histórico de DataDocumento para linhas já existentes; usa a data de entrega desejada como
-- aproximação só para não deixar a coluna nova nula em linhas antigas, depois torna obrigatória.
UPDATE `Demanda` SET `DataDocumento` = `DataEntregaDesejada` WHERE `DataDocumento` IS NULL;

ALTER TABLE `Demanda`
	MODIFY COLUMN `DataDocumento` DATETIME NOT NULL;
