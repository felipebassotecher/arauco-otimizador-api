-- Nova tabela de referência: nome legível de cada linha de produto. Produto.LinhaProdutoId e
-- Capacidade.LinhaProdutoId já usam esse id, mas sem nome legível associado até agora. Populada em
-- Script023 a partir da coluna linha_produto_nome de produtos.parquet (Data/Datasets/), com distinct
-- por linha_produto_id.
CREATE TABLE `LinhaProduto` (
	`LinhaProdutoId` INT NOT NULL,
	`Nome` VARCHAR(200) NOT NULL,
	PRIMARY KEY (`LinhaProdutoId`)
);
