-- ============================================
-- Arauco Hub API - Stored Procedures Script
-- Procedures úteis para operações do sistema
-- ============================================

-- Procedure para criar um novo usuário completo
CREATE PROCEDURE sp_CriarUsuarioCompleto
    @Nome VARCHAR(200),
    @Email VARCHAR(200),
    @NomeUsuario VARCHAR(100),
    @Matricula VARCHAR(50),
    @Cpf VARCHAR(14),
    @Celular VARCHAR(20),
    @EmpresaId INT,
    @CargoId INT = NULL,
    @PerfilId INT = 2, -- Usuário Padrão por default
    @TipoColaboradorEnum INT = 1 -- Empregado por default
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ColaboradorId INT;
    DECLARE @UsuarioId VARCHAR(100);
    
    BEGIN TRANSACTION;
    
    TRY
        -- Inserir colaborador
        INSERT INTO Colaborador (
            Nome, EmailComercial, Matricula, Cpf, Celular, 
            Ativo, Excluido, TipoColaboradorEnum, 
            EmailPortal, NomePortal, DataHoraCadastro,
            EmpresaId, CargoId, SituacaoColaboradorId
        ) VALUES (
            @Nome, @Email, @Matricula, @Cpf, @Celular,
            1, 0, @TipoColaboradorEnum,
            @Email, @Nome, GETDATE(),
            @EmpresaId, @CargoId, 1
        );
        
        SET @ColaboradorId = SCOPE_IDENTITY();
        SET @UsuarioId = 'user-' + CAST(@ColaboradorId AS VARCHAR(10));
        
        -- Inserir usuário
        INSERT INTO Usuario (
            UsuarioId, Email, Nome, NomeUsuario, 
            Ativo, AplicacaoEnum, ColaboradorId, PerfilId
        ) VALUES (
            @UsuarioId, @Email, @Nome, @NomeUsuario,
            1, 1, @ColaboradorId, @PerfilId
        );
        
        COMMIT TRANSACTION;
        
        SELECT @ColaboradorId as ColaboradorId, @UsuarioId as UsuarioId;
        
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;

-- Procedure para desativar usuário
CREATE PROCEDURE sp_DesativarUsuario
    @UsuarioId VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    
    TRY
        -- Desativar usuário
        UPDATE Usuario 
        SET Ativo = 0, DataHoraRemocao = GETDATE()
        WHERE UsuarioId = @UsuarioId;
        
        -- Desativar colaborador relacionado
        UPDATE Colaborador 
        SET Ativo = 0
        WHERE ColaboradorId = (
            SELECT ColaboradorId 
            FROM Usuario 
            WHERE UsuarioId = @UsuarioId
        );
        
        COMMIT TRANSACTION;
        
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;

-- Procedure para obter estatísticas de cartões por período
CREATE PROCEDURE sp_EstatisticasCartoesPorPeriodo
    @DataInicio DATETIME2,
    @DataFim DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        CASE TipoEnum 
            WHEN 1 THEN 'Segurança'
            WHEN 2 THEN 'Excelência e Inovação'
            WHEN 3 THEN 'Trabalho em Equipe'
            WHEN 4 THEN 'Bom Cidadão'
            WHEN 5 THEN 'Compromisso'
        END as TipoCartao,
        COUNT(*) as Quantidade,
        COUNT(DISTINCT Remetente) as RemetentesUnicos,
        COUNT(DISTINCT Destinatario) as DestinatariosUnicos
    FROM Cartao
    WHERE DataHoraCriacao BETWEEN @DataInicio AND @DataFim
    GROUP BY TipoEnum
    ORDER BY COUNT(*) DESC;
END;

-- Procedure para obter documentos pendentes de assinatura por colaborador
CREATE PROCEDURE sp_DocumentosPendentesAssinatura
    @ColaboradorId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        d.DocumentoId,
        td.Nome as TipoDocumento,
        d.DataHoraPublicacao,
        d.Versao
    FROM Documento d
        INNER JOIN TipoDocumento td ON d.TipoDocumentoId = td.TipoDocumentoId
        LEFT JOIN AssinaturaDocumento ad ON d.DocumentoId = ad.DocumentoId 
            AND ad.ColaboradorId = @ColaboradorId
    WHERE d.DataHoraPublicacao IS NOT NULL
        AND (d.DataHoraDesativacao IS NULL OR d.DataHoraDesativacao > GETDATE())
        AND ad.ColaboradorId IS NULL
    ORDER BY d.DataHoraPublicacao DESC;
END;

-- Procedure para registrar assinatura de documento
CREATE PROCEDURE sp_RegistrarAssinaturaDocumento
    @DocumentoId VARCHAR(100),
    @ColaboradorId INT,
    @EnderecoIp VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Verificar se já existe assinatura
    IF EXISTS (
        SELECT 1 
        FROM AssinaturaDocumento 
        WHERE DocumentoId = @DocumentoId AND ColaboradorId = @ColaboradorId
    )
    BEGIN
        RAISERROR('Colaborador já assinou este documento.', 16, 1);
        RETURN;
    END
    
    -- Verificar se documento está ativo
    IF NOT EXISTS (
        SELECT 1 
        FROM Documento 
        WHERE DocumentoId = @DocumentoId 
            AND DataHoraPublicacao IS NOT NULL
            AND (DataHoraDesativacao IS NULL OR DataHoraDesativacao > GETDATE())
    )
    BEGIN
        RAISERROR('Documento não está disponível para assinatura.', 16, 1);
        RETURN;
    END
    
    -- Registrar assinatura
    INSERT INTO AssinaturaDocumento (
        DocumentoId, ColaboradorId, DataHoraAssinatura, EnderecoIp
    ) VALUES (
        @DocumentoId, @ColaboradorId, GETDATE(), @EnderecoIp
    );
END;

-- Procedure para obter ranking de cartões por colaborador
CREATE PROCEDURE sp_RankingCartoesPorColaborador
    @DataInicio DATETIME2 = NULL,
    @DataFim DATETIME2 = NULL,
    @Top INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @DataInicio IS NULL SET @DataInicio = DATEADD(MONTH, -1, GETDATE());
    IF @DataFim IS NULL SET @DataFim = GETDATE();
    
    SELECT TOP (@Top)
        c.ColaboradorId,
        c.Nome,
        c.EmailComercial,
        e.Nome as NomeEmpresa,
        COUNT(ct.Id) as TotalCartoesRecebidos
    FROM Colaborador c
        LEFT JOIN Usuario u ON c.ColaboradorId = u.ColaboradorId
        LEFT JOIN Cartao ct ON u.UsuarioId = ct.DestinatarioId
            AND ct.DataHoraCriacao BETWEEN @DataInicio AND @DataFim
        LEFT JOIN Empresa e ON c.EmpresaId = e.EmpresaId
    WHERE c.Ativo = 1 AND c.Excluido = 0
    GROUP BY c.ColaboradorId, c.Nome, c.EmailComercial, e.Nome
    ORDER BY COUNT(ct.Id) DESC;
END;