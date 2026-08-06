using System.Runtime.Serialization;

namespace Arauco.Otimizador.Common.Domain.Enums.Criterio;

public enum OperadorCriterioEnum
{
    [EnumMember(Value = "igual_a")]
    IgualA = 1,

    [EnumMember(Value = "diferente_de")]
    DiferenteDe = 2,

    [EnumMember(Value = "maior_que")]
    MaiorQue = 3,

    [EnumMember(Value = "menor_que")]
    MenorQue = 4,

    [EnumMember(Value = "comeca_com")]
    ComecaCom = 5,

    [EnumMember(Value = "termina_com")]
    TerminaCom = 6
}