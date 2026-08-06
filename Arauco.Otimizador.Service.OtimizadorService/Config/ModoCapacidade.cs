namespace Arauco.Otimizador.Service.OtimizadorService;

public enum ModoCapacidade
{
    /// <summary>Capacidade real do tático, nas semanas em que ela existe.</summary>
    Real,

    /// <summary>Perfil semanal médio do tático mais recente, replicado a partir da semana de ancoragem.</summary>
    Simulada,

    /// <summary>Capacidade real redistribuída entre as plantas que têm linha física para cada linha de produto.</summary>
    Espalhada
}
