-- Aumenta o tamanho da coluna de identidade da tabela de demanda, que passa a utilizar IDs de
-- 12 caracteres (IdGenerator.New(12)/NewSync(12)) para evitar colisões — mesmo motivo e mesmo
-- ajuste já feito em PedidoOtimizado/PedidoOtimizadoNaoAlocado (Script008) e Pedido (Script009).

ALTER TABLE `Demanda` MODIFY COLUMN `DemandaId` CHAR(12) NOT NULL;
