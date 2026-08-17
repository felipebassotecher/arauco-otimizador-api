-- Adiciona ao Setup os parâmetros de horizonte/capacidade do motor de otimização (Config.cs):
-- horizonte, modo de capacidade (real/simulada), semana inicial e alvo de capacidade sobre demanda.
-- Espelham os defaults hard-coded do motor, mas ainda não são lidos por ele — Setup não é
-- referenciado por Cenário hoje (ver docs-fluxo-otimizacao.pdf para o desenho completo do motor).

ALTER TABLE `Setup`
	ADD COLUMN `Horizonte` INT NULL,
	ADD COLUMN `ModoCapacidadeId` INT NULL,
	ADD COLUMN `SemanaInicial` VARCHAR(10) NULL,
	ADD COLUMN `AlvoCapacidadeSobreDemanda` DECIMAL(5, 4) NULL;
