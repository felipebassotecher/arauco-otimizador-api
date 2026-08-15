-- Remove os campos de peso/variação/trocas do Setup (não fazem mais parte do formulário de
-- critérios) e adiciona a flag Ativo em SetupOrdemImportancia, para que critérios desativados
-- continuem sendo persistidos em sua posição (e não apenas os ativos).

ALTER TABLE `Setup`
	DROP COLUMN `PesoMinimoCarregamento`,
	DROP COLUMN `PercentualVariacaoMediaVenda`,
	DROP COLUMN `QuantidadeMaximaTrocas`,
	DROP COLUMN `UtilizarToleranciaPeso`,
	DROP COLUMN `PermitirCarregarAbaixoPesoMinimo`;

ALTER TABLE `SetupOrdemImportancia`
	ADD COLUMN `Ativo` BIT NOT NULL DEFAULT 1;
