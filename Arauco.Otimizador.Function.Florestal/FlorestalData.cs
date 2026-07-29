using CsvHelper.Configuration.Attributes;

namespace Arauco.Otimizador.Function.Florestal
{
    public class FlorestalData
    {
        [Index(0)]
        public string? Regiao { get; set; }

        [Index(1)]
        public string? PredioFazenda { get; set; }

        [Index(2)]
        public string? Gleba { get; set; }

        [Index(3)]
        public double? Precip_1_dia { get; set; }

        [Index(4)]
        public double? Precip_3_dias { get; set; }

        [Index(5)]
        public double? Precip_5_dias { get; set; }

        [Index(6)]
        public double? KpiMeteorologico { get; set; }

        [Index(7)]
        public string? Risco { get; set; }
    }
}
