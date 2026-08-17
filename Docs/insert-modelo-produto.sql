-- Modelo de INSERT para a tabela `Produto` — master data consumida pelo motor de otimização
-- (Dados/Carregador.cs). Representa um SKU (chapa). Sem FK — carga de referência independente.
--
-- Colunas:
--   ProdutoId          VARCHAR(50) NOT NULL PK   código do produto/material (mesmo valor usado em
--                                                 Elegibilidade.ProdutoId e Demanda.Material)
--   Descricao          VARCHAR(500) NULL         descrição livre do SKU
--   LinhaProdutoId     INT NOT NULL              linha de produto — é a chave que casa Produto com
--                                                 Capacidade.LinhaProdutoId (não é o mesmo conceito
--                                                 que Capacidade.LinhaProducaoId, que é a linha de
--                                                 produção física da planta)
--   LoteMinimoChapas   DECIMAL(18,4) NOT NULL    lote mínimo do produto, em quantidade de chapas —
--                                                 usado para excluir do motor demandas cujo volume
--                                                 não atinge o lote mínimo (Modelo/Preparacao.cs)
--   LarguraMm          DECIMAL(18,4) NOT NULL    dimensão da chapa (mm)
--   ComprimentoMm      DECIMAL(18,4) NOT NULL    dimensão da chapa (mm)
--   EspessuraMm        DECIMAL(18,4) NOT NULL    dimensão da chapa (mm) — as três dimensões juntas
--                                                 formam o volume de uma chapa em m³
--                                                 (Largura/1000 × Comprimento/1000 × Espessura/1000),
--                                                 usado no cálculo do piso de fragmentação do item
--   Ativo              BIT NOT NULL              0/1 — produtos inativos não entram no motor
--
-- Para importação em massa: gere um INSERT por lote de linhas (não é preciso um INSERT por linha).
-- Valor nulo de lote mínimo na origem deve virar 0.0000, não NULL (a coluna é NOT NULL).

INSERT INTO `Produto`
    (`ProdutoId`, `Descricao`, `LinhaProdutoId`, `LoteMinimoChapas`, `LarguraMm`, `ComprimentoMm`, `EspessuraMm`, `Ativo`)
VALUES
    ('1200055', 'MDP ST LX E2-- 5500x2200x25,0MM P/I1/140', 17, 50.0000, 2200.0000, 5500.0000, 25.0000, 1),
    ('1200056', 'MDP ST LX E2-- 5500x2200x15,0MM P/I1/230', 17, 50.0000, 2200.0000, 5500.0000, 15.0000, 1),
    ('1202575', 'MDF CP NL E2-- 3900X2440X15,0MM P/I1/230',  9,  1.0000, 2440.0000, 3900.0000, 15.0000, 1);
    -- (adicione uma linha por produto do lote)
