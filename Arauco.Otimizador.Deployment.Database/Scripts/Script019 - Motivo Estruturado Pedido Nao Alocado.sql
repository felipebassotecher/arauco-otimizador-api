-- Motivo estruturado (categoria + motivo) da não-alocação, na mesma linguagem já usada para os
-- motivos de alocação dos pedidos (ver Script018). Colunas NULL porque execuções de otimização
-- anteriores a este recurso não têm esse dado — a coluna `Motivo` (texto livre) continua sendo
-- preenchida em paralelo. Gerado em Otimizacao.Resolver e persistido em OtimizadorService.OtimizarAsync.
ALTER TABLE `PedidoOtimizadoNaoAlocado`
	ADD COLUMN `CategoriaId` INT NULL,
	ADD COLUMN `MotivoId` INT NULL;
