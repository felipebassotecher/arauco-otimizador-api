-- Modelo de INSERT para a tabela `Centro` — master data consumida pelo motor de otimização
-- (Dados/Carregador.cs). Representa uma planta/fábrica. Sem FK — carga de referência independente.
--
-- Colunas:
--   CentroId              INT NOT NULL PK        identificador numérico da planta (mesmo valor usado
--                                                 em Elegibilidade.CentroId e Capacidade.CentroId)
--   Codigo                VARCHAR(50) NOT NULL    código curto da planta (ex.: "PB02")
--   Nome                  VARCHAR(200) NOT NULL   nome legível, exibido na interface
--   Ativo                 BIT NOT NULL            0/1 — só plantas ativas entram no motor de otimização
--   PorcentagemIndustria  INT NOT NULL            % do mix de clientes Indústria atendido por essa
--                                                 planta (informativo/relatório — o motor não lê este
--                                                 campo; a classificação Indústria/Revenda de cada
--                                                 pedido vem do Segmento da demanda, não daqui)
--   PorcentagemRevenda    INT NOT NULL            idem, para Revenda — não precisa somar 100 com o
--                                                 campo acima, mas é o padrão nos dados existentes
--
-- Para importação em massa: gere um INSERT por lote de linhas (não é preciso um INSERT por linha).

INSERT INTO `Centro`
    (`CentroId`, `Codigo`, `Nome`, `Ativo`, `PorcentagemIndustria`, `PorcentagemRevenda`)
VALUES
    (1, 'PB02', 'Jaguariaíva',  1, 40, 60),
    (2, 'PB06', 'Piên',         1, 50, 50),
    (3, 'PB22', 'Ponta Grossa', 1, 90, 10);
    -- (adicione uma linha por planta do lote)
