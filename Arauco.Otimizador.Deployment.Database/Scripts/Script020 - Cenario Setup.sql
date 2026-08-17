-- O cenário deixa de ter critérios próprios: toda a configuração do motor de otimização (horizonte,
-- capacidade, carreta, limite de recebimento, ordem de importância dos critérios) passa a vir do
-- Setup vinculado (ver OtimizadorService.CriarConfig). O vínculo é escolhido na criação do cenário e
-- é imutável depois. `SetupId` fica NULL-ável na coluna porque cenários já existentes não têm setup —
-- eles simplesmente não conseguem rodar /otimizar até serem recriados (ambiente de desenvolvimento,
-- sem dado de produção).
ALTER TABLE `Cenario`
	ADD COLUMN `SetupId` CHAR(6) NULL,
	ADD CONSTRAINT `FK_Cenario_Setup` FOREIGN KEY (`SetupId`) REFERENCES `Setup` (`SetupId`);

DROP TABLE `CenarioCriterio`;
