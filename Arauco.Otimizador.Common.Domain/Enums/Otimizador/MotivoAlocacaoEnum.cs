using System.Runtime.Serialization;

namespace Arauco.Otimizador.Common.Domain.Enums.Otimizador;

// Motivos possíveis de "por que este pedido foi alocado onde está" (categoria em
// CategoriaMotivoEnum) e de "por que não foi alocado" (usado só em
// PedidoOtimizadoNaoAlocado.Motivo, texto livre — não trafega via API). Computados como
// heurísticas estruturais sobre o resultado do CP-SAT (Otimizacao.cs) — o solver em si não expõe
// "razão" para uma escolha ótima entre alternativas empatadas, então são leituras honestas dos
// dados de entrada/capacidade, não uma reconstrução literal do raciocínio do solver.
public enum MotivoAlocacaoEnum
{
    // Categoria PorqueNestaSemana
    [EnumMember(Value = "sem_capacidade_semanas_anteriores")]
    SemCapacidadeSemanasAnteriores,

    [EnumMember(Value = "lote_minimo_nao_cabe_antes")]
    LoteMinimoNaoCabeAntes,

    [EnumMember(Value = "primeira_semana_horizonte")]
    PrimeiraSemanaHorizonte,

    // Categoria PorqueNesteCentro
    [EnumMember(Value = "planta_escolhida_entre_elegiveis")]
    PlantaEscolhidaEntreElegiveis,

    [EnumMember(Value = "unica_planta_elegivel_com_capacidade")]
    UnicaPlantaElegivelComCapacidade,

    // "Porque não alocado" — só em PedidoOtimizadoNaoAlocado.Motivo (texto livre), não em
    // PedidoOtimizadoMotivo (que só guarda motivo de pedido efetivamente alocado).
    [EnumMember(Value = "despriorizado")]
    Despriorizado,

    [EnumMember(Value = "lote_minimo_maior_que_parametrizado")]
    LoteMinimoMaiorQueParametrizado
}
