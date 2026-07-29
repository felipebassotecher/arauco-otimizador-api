CREATE TABLE `Cartao` (
	`CartaoId` CHAR(6) NOT NULL,
	`TipoCartaoId` INT NOT NULL,
	`DataHoraCriacao` DATETIME NOT NULL,
	`Mensagem` VARCHAR(500) NOT NULL,
	`ColaboradorId_Remetente` INT NOT NULL,
	`ColaboradorId_Destinatario` INT NOT NULL,
	PRIMARY KEY (`CartaoId`)
);
