using Arauco.Otimizador.Common.Domain.Enums.Cartao;

namespace Arauco.Otimizador.Common.Domain.Models.Cartao
{
    public class CartaoListaModel
    {
        public string Id { get; set; }
        public CartaoTipoEnum Tipo { get; set; }
        public DateTime DataHoraCriacao { get; set; }
        public CartaoStatusEnum Status { get; set; }
    }
}
