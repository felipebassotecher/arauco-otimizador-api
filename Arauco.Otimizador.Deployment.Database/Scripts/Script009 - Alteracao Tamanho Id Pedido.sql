-- Aumenta o tamanho das colunas de identidade das tabelas de resultado do motor de otimização,
-- que passam a utilizar IDs de 12 caracteres (IdGenerator.New(12)) para evitar colisões.

ALTER TABLE `Pedido` MODIFY COLUMN `PedidoId` CHAR(12) NOT NULL;
