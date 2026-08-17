using System.Runtime.Serialization;

namespace Arauco.Otimizador.Common.Domain.Enums.Setup;

// Espelha (só os 2 modos expostos no Setup) o ModoCapacidade do motor de otimização
// (Arauco.Otimizador.Service.OtimizadorService/Config/ModoCapacidade.cs) — Espalhada não é oferecida
// aqui por decisão de produto. Os valores numéricos (0/1) são os mesmos do motor de propósito, para
// que este campo possa alimentar OtimizacaoRequest.Capacidade diretamente no futuro, sem conversão.
public enum ModoCapacidadeEnum
{
    [EnumMember(Value = "real")]
    Real = 0,

    [EnumMember(Value = "simulada")]
    Simulada = 1
}
