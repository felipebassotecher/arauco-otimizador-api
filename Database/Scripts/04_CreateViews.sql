-- ============================================
-- Arauco Hub API - Database Views Script
-- Views úteis para consultas do sistema
-- ============================================

-- View para colaboradores ativos com informações completas
CREATE VIEW vw_ColaboradoresAtivos AS
SELECT 
    c.ColaboradorId,
    c.Nome,
    c.EmailComercial,
    c.EmailPortal,
    c.Matricula,
    c.Cpf,
    c.Celular,
    c.NumeroCracha,
    CASE c.TipoColaboradorEnum 
        WHEN 1 THEN 'Empregado'
        WHEN 2 THEN 'Terceiro'
        ELSE 'Não Definido'
    END as TipoColaborador,
    e.Nome as NomeEmpresa,
    cg.Nome as NomeCargo,
    cc.Nome as NomeCentroCusto,
    pt.Nome as NomePostoTrabalho,
    sc.Nome as SituacaoColaborador,
    gestor.Nome as NomeGestor
FROM Colaborador c
    LEFT JOIN Empresa e ON c.EmpresaId = e.EmpresaId
    LEFT JOIN Cargo cg ON c.CargoId = cg.CargoId
    LEFT JOIN CentroCusto cc ON c.CentroCustoId = cc.CentroCustoId
    LEFT JOIN PostoTrabalho pt ON c.PostoTrabalhoId = pt.PostoTrabalhoId
    LEFT JOIN SituacaoColaborador sc ON c.SituacaoColaboradorId = sc.SituacaoColaboradorId
    LEFT JOIN Colaborador gestor ON c.ColaboradorId_Gestor = gestor.ColaboradorId
WHERE c.Ativo = 1 AND c.Excluido = 0;

-- View para usuários ativos com informações de perfil e colaborador
CREATE VIEW vw_UsuariosAtivos AS
SELECT 
    u.UsuarioId,
    u.Email,
    u.Nome,
    u.NomeUsuario,
    p.Nome as NomePerfil,
    c.Matricula,
    c.EmailComercial,
    e.Nome as NomeEmpresa,
    CASE u.AplicacaoEnum 
        WHEN 1 THEN 'Hub'
        ELSE 'Não Definido'
    END as Aplicacao
FROM Usuario u
    INNER JOIN Perfil p ON u.PerfilId = p.PerfilId
    INNER JOIN Colaborador c ON u.ColaboradorId = c.ColaboradorId
    LEFT JOIN Empresa e ON c.EmpresaId = e.EmpresaId
WHERE u.Ativo = 1 AND u.DataHoraRemocao IS NULL;

-- View para documentos com informações de tipo e usuário publicador
CREATE VIEW vw_DocumentosPublicados AS
SELECT 
    d.DocumentoId,
    d.DataHoraCriacao,
    d.DataHoraPublicacao,
    d.Versao,
    td.Nome as TipoDocumento,
    u.Nome as UsuarioPublicacao,
    COUNT(ad.ColaboradorId) as TotalAssinaturas
FROM Documento d
    INNER JOIN TipoDocumento td ON d.TipoDocumentoId = td.TipoDocumentoId
    LEFT JOIN Usuario u ON d.UsuarioId_Publicacao = u.UsuarioId
    LEFT JOIN AssinaturaDocumento ad ON d.DocumentoId = ad.DocumentoId
WHERE d.DataHoraPublicacao IS NOT NULL 
    AND (d.DataHoraDesativacao IS NULL OR d.DataHoraDesativacao > GETDATE())
GROUP BY d.DocumentoId, d.DataHoraCriacao, d.DataHoraPublicacao, d.Versao, td.Nome, u.Nome;

-- View para cartões com informações de remetente e destinatário
CREATE VIEW vw_CartoesDetalhados AS
SELECT 
    c.Id,
    CASE c.TipoEnum 
        WHEN 1 THEN 'Segurança'
        WHEN 2 THEN 'Excelência e Inovação'
        WHEN 3 THEN 'Trabalho em Equipe'
        WHEN 4 THEN 'Bom Cidadão'
        WHEN 5 THEN 'Compromisso'
        ELSE 'Não Definido'
    END as TipoCartao,
    c.DataHoraCriacao,
    c.Mensagem,
    ur.Nome as NomeRemetente,
    ud.Nome as NomeDestinatario,
    cr.EmailComercial as EmailRemetente,
    cd.EmailComercial as EmailDestinatario
FROM Cartao c
    LEFT JOIN Usuario ur ON c.RemetenteId = ur.UsuarioId
    LEFT JOIN Usuario ud ON c.DestinatarioId = ud.UsuarioId
    LEFT JOIN Colaborador cr ON ur.ColaboradorId = cr.ColaboradorId
    LEFT JOIN Colaborador cd ON ud.ColaboradorId = cd.ColaboradorId;

-- View para estatísticas de assinaturas por documento
CREATE VIEW vw_EstatisticasAssinaturas AS
SELECT 
    d.DocumentoId,
    td.Nome as TipoDocumento,
    d.Versao,
    d.DataHoraPublicacao,
    COUNT(ad.ColaboradorId) as TotalAssinaturas,
    MIN(ad.DataHoraAssinatura) as PrimeiraAssinatura,
    MAX(ad.DataHoraAssinatura) as UltimaAssinatura
FROM Documento d
    INNER JOIN TipoDocumento td ON d.TipoDocumentoId = td.TipoDocumentoId
    LEFT JOIN AssinaturaDocumento ad ON d.DocumentoId = ad.DocumentoId
WHERE d.DataHoraPublicacao IS NOT NULL
GROUP BY d.DocumentoId, td.Nome, d.Versao, d.DataHoraPublicacao;

-- View para regras por perfil
CREATE VIEW vw_RegrasPerPerfil AS
SELECT 
    p.PerfilId,
    p.Nome as NomePerfil,
    r.RegraEnum,
    r.Nome as NomeRegra,
    r.Sistema,
    r.Categoria
FROM Perfil p
    INNER JOIN PerfilRegra pr ON p.PerfilId = pr.PerfilId
    INNER JOIN Regra r ON pr.RegraEnum = r.RegraEnum
WHERE p.Excluido = 0;