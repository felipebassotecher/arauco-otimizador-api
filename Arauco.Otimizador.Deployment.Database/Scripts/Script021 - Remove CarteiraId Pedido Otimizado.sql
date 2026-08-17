-- CarteiraId (Script017) ligava cada pedido de volta a exatamente uma demanda, para o desconto de
-- pinados por igualdade exata. Com itens divisíveis (um item agora agrega várias demandas do mesmo
-- cliente+produto — ver Preparacao.cs), uma fração alocada de um item não corresponde mais a uma
-- CarteiraId específica; o desconto de pinados passa a ser por grupo (cliente, produto) — ver
-- OtimizadorService.DescontarPinados.
ALTER TABLE `PedidoOtimizado` DROP COLUMN `CarteiraId`;
