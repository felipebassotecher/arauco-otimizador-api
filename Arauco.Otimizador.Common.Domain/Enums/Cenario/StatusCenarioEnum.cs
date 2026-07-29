using System.Runtime.Serialization;

namespace Arauco.Otimizador.Common.Domain.Enums.Cenario;

public enum StatusCenarioEnum
{
    [EnumMember(Value = "pendente")]
    Pendente = 1,

    [EnumMember(Value = "processando")]
    Processando = 2,

    [EnumMember(Value = "processado")]
    Processado = 3,

    [EnumMember(Value = "submetido")]
    Submetido = 4
}
