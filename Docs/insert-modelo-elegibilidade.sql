-- Modelo de INSERT para a tabela `Elegibilidade` — master data consumida pelo motor de otimização
-- (Dados/Carregador.cs). Cada linha diz "este produto PODE ser alocado nesta planta". Sem FK
-- (ProdutoId/CentroId não referenciam Produto/Centro por constraint) — carga de referência
-- independente, mas os valores devem existir em Produto.ProdutoId e Centro.CentroId para o motor
-- de fato usar a linha (senão o item é excluído no pré-flight por "SemElegibilidade").
--
-- Colunas:
--   Id         INT NOT NULL AUTO_INCREMENT PK   gerado pelo banco — NÃO informar no INSERT
--   ProdutoId  VARCHAR(50) NOT NULL              código do produto (Produto.ProdutoId)
--   CentroId   INT NOT NULL                      identificador da planta (Centro.CentroId)
--
-- Um produto elegível em várias plantas gera uma linha por combinação (produto, planta) — não é
-- uma lista dentro de uma única linha. Repetir a mesma combinação (produto, planta) mais de uma vez
-- não quebra nada (o motor não faz distinct explícito, mas também não duplica a decisão de alocação
-- por causa disso), mas não tem efeito adicional — evite duplicar ao montar o lote.
--
-- Para importação em massa: gere um INSERT por lote de linhas (não é preciso um INSERT por linha).

INSERT INTO `Elegibilidade`
    (`ProdutoId`, `CentroId`)
VALUES
    ('1200055', 1),
    ('1200055', 2),
    ('1200055', 3),
    ('1200056', 1),
    ('1200056', 2),
    ('1202575', 3);
    -- (adicione uma linha por combinação produto+planta elegível do lote)
