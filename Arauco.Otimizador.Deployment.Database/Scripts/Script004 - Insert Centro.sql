-- Popula a tabela `Centro` a partir de Data/Datasets/centros.parquet

INSERT INTO `Centro` (`CentroId`, `Codigo`, `Nome`, `Ativo`, `PorcentagemIndustria`, `PorcentagemRevenda`) VALUES
(1, 'PB02', 'Jaguariaiva', 1, 40, 60),
(2, 'PB06', 'Piên', 1, 50, 50),
(3, 'PB22', 'Ponta Grossa', 1, 90, 10),
(4, 'PB23', 'Montenegro', 1, 50, 50),
(5, 'PB25', 'Jaguariaiva (Cópia)', 0, 40, 60),
(6, 'PB26', 'Piên (Cópia)', 0, 50, 50);
