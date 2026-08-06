using System.Runtime.Serialization;

namespace Arauco.Otimizador.Common.Domain.Enums.Criterio;

public enum TipoCriterioEnum
{
    [EnumMember(Value = "string")]
    String = 1,

    [EnumMember(Value = "numerico")]
    Numerico = 2
}