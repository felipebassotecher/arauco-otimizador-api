-- Adiciona o tipo de cliente (Indústria/Revenda) ao pedido otimizado, para exibir na tabela de
-- visualização por semana junto com o tipo de frete. Mesma resolução já usada pelo critério
-- "Tipo de Cliente" do motor de otimização (AvaliadorCriterios/Item.Industria).

ALTER TABLE `PedidoOtimizado` ADD COLUMN `Industria` BIT NOT NULL DEFAULT 0;
