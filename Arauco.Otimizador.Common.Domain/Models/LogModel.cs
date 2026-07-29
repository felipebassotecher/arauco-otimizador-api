using Arauco.Otimizador.Common.Domain.Enums;

namespace Arauco.Otimizador.Common.Domain.Models
{
    public class LogModel
    {
        public string LogId { get; set; }
        public string Agrupador { get; set; }
        public TipoLogEnum TipoLogEnum { get; set; }
        public DateTime DataHora { get; set; }
        public string Descricao { get; set; }

    }
}
