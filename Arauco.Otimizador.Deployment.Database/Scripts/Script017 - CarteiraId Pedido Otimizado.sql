-- Liga cada pedido otimizado de volta à demanda (Demanda.CarteiraId) que o originou. Necessário
-- para descontar com precisão, numa reotimização, a demanda cujo pedido já está pinado — sem isso,
-- duas demandas do mesmo cliente+produto seriam indistinguíveis para esse desconto (ver
-- OtimizadorService.DescontarPinados).

ALTER TABLE `PedidoOtimizado` ADD COLUMN `CarteiraId` BIGINT NOT NULL DEFAULT 0;
