-- Modelo de INSERT para a tabela `Capacidade` — master data consumida pelo motor de otimização
-- (Dados/Carregador.cs). Cada linha é a capacidade declarada de uma planta, para uma linha de
-- produto, numa semana ISO específica. Sem FK — carga de referência independente.
--
-- Colunas:
--   Id                INT NOT NULL AUTO_INCREMENT PK   gerado pelo banco — NÃO informar no INSERT
--   CentroId          INT NOT NULL                      identificador da planta (Centro.CentroId)
--   LinhaProducaoId   INT NOT NULL                      linha de PRODUÇÃO física da planta (id
--                                                        interno da fábrica) — NÃO confundir com
--                                                        LinhaProdutoId abaixo; o motor só usa este
--                                                        campo para agrupamento/relatório, não para
--                                                        decidir elegibilidade
--   LinhaProdutoId    INT NOT NULL                      linha de PRODUTO — precisa bater com
--                                                        Produto.LinhaProdutoId; é a chave que o motor
--                                                        usa para achar capacidade de um item
--   SemanaIso         INT NOT NULL                      número da semana ISO (1–53)
--   Ano               INT NOT NULL                      ano civil do Mes abaixo — se a semana ISO
--                                                        cair na virada do ano (ex.: Mes=12,
--                                                        SemanaIso=1, ou Mes=1, SemanaIso=52/53), o
--                                                        motor corrige automaticamente o ano da semana
--                                                        na leitura; não é preciso pré-calcular isso
--                                                        na carga
--   Mes               INT NOT NULL                      mês civil de referência da linha (1–12)
--   TipoAlocacao      INT NOT NULL                      *** usar sempre 1 (Mercado Interno) *** — o
--                                                        motor descarta silenciosamente qualquer linha
--                                                        com TipoAlocacao <> 1
--                                                        (Dados/Carregador.CarregarCapacidadeAsync)
--   Quantidade        BIGINT NOT NULL                   capacidade da semana, na mesma unidade de
--                                                        volume usada pelo restante do motor (m³)
--   DataCriacao       DATETIME NULL                     data/hora da extração de origem — usada só
--                                                        para desempate quando duas linhas caem na
--                                                        mesma chave (CentroId, LinhaProducaoId,
--                                                        LinhaProdutoId, semana): a mais recente vence
--
-- Para importação em massa: gere um INSERT por lote de linhas (não é preciso um INSERT por linha).

INSERT INTO `Capacidade`
    (`CentroId`, `LinhaProducaoId`, `LinhaProdutoId`, `SemanaIso`, `Ano`, `Mes`, `TipoAlocacao`, `Quantidade`, `DataCriacao`)
VALUES
    (1, 2, 17, 40, 2025, 10, 1, 3500, '2025-10-16 18:39:00'),
    (1, 2, 17, 41, 2025, 10, 1, 4375, '2025-10-16 18:39:00'),
    (1, 2, 17, 42, 2025, 10, 1, 4375, '2025-10-16 18:39:00'),
    (3, 9,  9, 40, 2025, 10, 1,  168, '2025-10-16 18:39:00');
    -- (adicione uma linha por combinação planta+linha de produto+semana do lote)
