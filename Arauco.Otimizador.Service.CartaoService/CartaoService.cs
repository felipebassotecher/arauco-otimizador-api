using Arauco.Otimizador.Common.Domain.Enums.Cartao;
using Arauco.Otimizador.Common.Domain.Models.Cartao;
using Arauco.Otimizador.Common.Domain.Services.Cartao;
using Arauco.Otimizador.Common.Domain.Session;
using Arauco.Otimizador.Data.Entities;
using Arauco.Otimizador.Data.Entities.Cartao;
using Arauco.Otimizador.Service.Base;
using Microsoft.EntityFrameworkCore;
using Techer.Common.Domain.Exceptions;
using Techer.Common.Domain.Interfaces;
using Techer.Common.Id;

namespace Arauco.Otimizador.Service.CartaoService;

public class CartaoService : ServiceBase, ICartaoService
{
    public CartaoService(IUnitOfWork unitOfWork, IEnvironmentVariables environmentVariables) : base(unitOfWork, environmentVariables)
    {

    }

    public async Task<List<CartaoListaModel>> ListarAsync(AppSessionModel session)
    {
        var cartoes = await unitOfWork
            .CartaoRepository
            .Where(c => c.ColaboradorId_Destinatario == session.ColaboradorId || c.ColaboradorId_Remetente == session.ColaboradorId)
            .Select(c => new CartaoListaModel
            {
                Id = c.CartaoId,
                DataHoraCriacao = c.DataHoraCriacao,
                Status = _StatusDoCartao(c.ColaboradorId_Remetente, session),
                Tipo = c.TipoEnum
            })
            .ToListAsync();

        return cartoes;
    }

    public async Task<string> NovoAsync(CartaoNovoModel model, AppSessionModel session)
    {
        var remetente = new
        {
            ColaboradorId = 0,
            Nome = ""
        };

        var destinatario = new
        {
            ColaboradorId = 1,
            Nome = ""
        };

        if (model.DestinatarioId.HasValue && destinatario == null)
        {
            throw new NotFoundException("Destinatário não encontrado");
        }

        var nomePersonalizado = model.DestinatarioId != null ? model.NomeSocial : model.NomeColaboradorExterno;

        var cartao = new Cartao
        {
            CartaoId = await IdGenerator.New(),
            DataHoraCriacao = DateTime.UtcNow,
            Mensagem = model.Mensagem,
            TipoEnum = model.Tipo,
            ColaboradorId_Remetente = session.ColaboradorId,
            Nome_Remetente = remetente.Nome,
            ColaboradorId_Destinatario = model.DestinatarioId,
            Nome_Destinatario = destinatario?.Nome,
            NomeDestinatarioPersonalizado = nomePersonalizado
        };

        unitOfWork.CartaoRepository.Add(cartao);
        await unitOfWork.SaveAsync();

        return cartao.CartaoId;
    }

    public async Task<CartaoDetalheModel> ObterAsync(string cartaoId, AppSessionModel session)
    {
        var cartao = await unitOfWork
            .CartaoRepository
            .Where(c => c.CartaoId == cartaoId)
            .Select(c => new
            {
                Id = c.CartaoId,
                RemetenteId = c.ColaboradorId_Remetente,
                DestinatarioId = c.ColaboradorId_Destinatario,
                c.NomeDestinatarioPersonalizado,
                c.DataHoraCriacao,
                c.Mensagem,
                Tipo = c.TipoEnum,
                Status = _StatusDoCartao(c.ColaboradorId_Remetente, session)
            })
            .FirstOrDefaultAsync() ?? throw new NotFoundException("Cartão não encontrado");

        var remetente = new KeyValuePair<int, string>(0, "");

        var destinatario = new KeyValuePair<int, string>(1, "");

        var model = new CartaoDetalheModel
        {
            Id = cartao.Id,
            DataHoraCriacao = cartao.DataHoraCriacao,
            Remetente = remetente,
            Destinatario = destinatario,
            NomeDestinatarioPersonalizado = cartao.NomeDestinatarioPersonalizado,
            Mensagem = cartao.Mensagem,
            Status = cartao.Status,
            Tipo = cartao.Tipo
        };

        return model;
    }

    private static CartaoStatusEnum _StatusDoCartao(int remetenteId, AppSessionModel session)
    {
        if (remetenteId == session.ColaboradorId)
            return CartaoStatusEnum.Enviado;

        return CartaoStatusEnum.Recebido;
    }
}
