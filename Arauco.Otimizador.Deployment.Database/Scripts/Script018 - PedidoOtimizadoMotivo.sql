-- Motivos de alocação: por que um PedidoOtimizado ficou na semana/centro em que ficou. Gerado pelo
-- CP-SAT em Otimizacao.Resolver (heurístico, não um traço literal do solver) e persistido no momento
-- do processamento para consulta posterior no detalhe do pedido (ver OtimizadorService.OtimizarAsync).
-- Um pedido pode ter vários motivos (uma linha por combinação categoria+motivo).
CREATE TABLE `PedidoOtimizadoMotivo` (
	`Id` INT NOT NULL AUTO_INCREMENT,
	`PedidoId` CHAR(12) NOT NULL,
	`CategoriaId` INT NOT NULL,
	`MotivoId` INT NOT NULL,
	PRIMARY KEY (`Id`),
	KEY `IX_PedidoOtimizadoMotivo_PedidoId` (`PedidoId`),
	CONSTRAINT `FK_PedidoOtimizadoMotivo_Pedido` FOREIGN KEY (`PedidoId`) REFERENCES `PedidoOtimizado` (`PedidoId`) ON DELETE CASCADE
);
