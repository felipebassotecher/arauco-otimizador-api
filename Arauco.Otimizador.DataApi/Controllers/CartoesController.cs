using Arauco.Otimizador.Common.Domain.Enums.Cartao;
using Arauco.Otimizador.Data.Entities;
using Arauco.Otimizador.DataApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;

namespace Arauco.Otimizador.DataApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CartoesController : ODataController
    {
        private readonly IUnitOfWork unitOfWork;

        public CartoesController(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        [HttpGet]
        [EnableQuery]
        public IActionResult Get()
        {
            var cartoes = unitOfWork.CartaoRepository
                .AsQueryable()
                .AsNoTracking()
                .Select(c => new CartaoODataModel
                {
                    CartaoId = c.CartaoId,
                    Tipo = c.TipoEnum.ToString(),
                    TipoDescricao =
                        c.TipoEnum == CartaoTipoEnum.Seguranca ? "Segurança" :
                        c.TipoEnum == CartaoTipoEnum.ExcelenciaEInovacao ? "Excelência e Inovação" :
                        c.TipoEnum == CartaoTipoEnum.TrabalhoEmEquipe ? "Trabalho em Equipe" :
                        c.TipoEnum == CartaoTipoEnum.BomCidadao ? "Bom Cidadão" :
                        c.TipoEnum == CartaoTipoEnum.Compromisso ? "Compromisso" : "",
                    DataHoraCriacao = c.DataHoraCriacao,
                    Mensagem = c.Mensagem,
                    ColaboradorId_Remetente = c.ColaboradorId_Remetente,
                    Nome_Remetente = c.Nome_Remetente,
                    CentroCusto_Remetente = c.CentroCusto_Remetente,
                    Cidade_Remetente = c.Cidade_Remetente,
                    Cargo_Remetente = c.Cargo_Remetente,
                    ColaboradorId_Destinatario = c.ColaboradorId_Destinatario,
                    Nome_Destinatario = c.Nome_Destinatario,
                    CentroCusto_Destinatario = c.CentroCusto_Destinatario,
                    Cidade_Destinatario = c.Cidade_Destinatario,
                    Cargo_Destinatario = c.Cargo_Destinatario,
                    NomeDestinatarioPersonalizado = c.NomeDestinatarioPersonalizado
                });

            return Ok(cartoes);
        }

        [HttpGet("{key}")]
        [EnableQuery]
        public IActionResult Get(string key)
        {
            var cartao = unitOfWork.CartaoRepository.FirstOrDefault(r => r.CartaoId == key);

            if (cartao == null)
                return NotFound();

            var result = new CartaoODataModel
            {
                CartaoId = cartao.CartaoId,
                Tipo = cartao.TipoEnum.ToString(),
                TipoDescricao = CartaoODataModel.GetTipoDescricao(cartao.TipoEnum),
                DataHoraCriacao = cartao.DataHoraCriacao,
                Mensagem = cartao.Mensagem,
                ColaboradorId_Remetente = cartao.ColaboradorId_Remetente,
                Nome_Remetente = cartao.Nome_Remetente,
                CentroCusto_Remetente = cartao.CentroCusto_Remetente,
                Cidade_Remetente = cartao.Cidade_Remetente,
                Cargo_Remetente = cartao.Cargo_Remetente,
                ColaboradorId_Destinatario = cartao.ColaboradorId_Destinatario,
                Nome_Destinatario = cartao.Nome_Destinatario,
                CentroCusto_Destinatario = cartao.CentroCusto_Destinatario,
                Cidade_Destinatario = cartao.Cidade_Destinatario,
                Cargo_Destinatario = cartao.Cargo_Destinatario,
                NomeDestinatarioPersonalizado = cartao.NomeDestinatarioPersonalizado
            };

            return Ok(result);
        }
    }
}
