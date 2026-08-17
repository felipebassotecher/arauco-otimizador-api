-- Relacionamento Produto -> LinhaProduto. Seguro porque os dois vêm da mesma extração
-- (produtos.parquet): todo LinhaProdutoId usado em Produto está, por construção, entre os 14
-- distintos carregados em LinhaProduto (Script023).
--
-- NÃO foi adicionada a mesma FK em Capacidade.LinhaProdutoId de propósito: capacidade.parquet usa
-- LinhaProdutoId 10 e 11 em várias linhas, e nenhum dos dois aparece em produtos.parquet — ou seja,
-- no dado real hoje, essa FK quebraria a carga existente de `Capacidade` (Script007). Capacidade
-- continua referenciando LinhaProdutoId de forma apenas convencional (sem constraint), mesmo padrão
-- informal que já valia para todas as tabelas de dataset antes desta mudança.
ALTER TABLE `Produto`
	ADD CONSTRAINT `FK_Produto_LinhaProduto` FOREIGN KEY (`LinhaProdutoId`) REFERENCES `LinhaProduto` (`LinhaProdutoId`);
