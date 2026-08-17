-- Popula `LinhaProduto` a partir de Data/Datasets/produtos.parquet — distinct de
-- (linha_produto_id, linha_produto_nome) sobre as 5452 linhas do arquivo (14 linhas de produto
-- distintas, sem conflito de nome por id e sem nulos).

INSERT INTO `LinhaProduto` (`LinhaProdutoId`, `Nome`) VALUES
(1, 'Substrato MDF'),
(2, 'Substrato MDP'),
(3, 'MDF Rev Madeirad'),
(4, 'MDF Rev Branco'),
(5, 'MDF Rev Unicor'),
(6, 'MDP Rev Branco'),
(7, 'MDP Rev Madeirad'),
(8, 'MDP Rev Unicor'),
(9, 'MDF Cru Std'),
(12, 'MDF Pintado Bco'),
(13, 'MDF Pintado Mad'),
(14, 'MDF Pintado Unic'),
(17, 'MDP Cru'),
(18, 'Substrato MDF 2.8');
