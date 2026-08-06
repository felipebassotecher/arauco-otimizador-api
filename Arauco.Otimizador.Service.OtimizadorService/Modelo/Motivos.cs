namespace Arauco.Otimizador.Service.OtimizadorService.Modelo;

public static class Motivos
{
    public const string PrimeiraSemana = "PRIMEIRA SEMANA DO HORIZONTE";
    public const string SemCapacidadeAntes = "SEM CAPACIDADE nas semanas anteriores";
    public const string DeslocamentoPossivel = "HAVIA FOLGA ANTES — só sairia deslocando outro pedido";
    public const string LoteMinimoNaoCabe = "LOTE MINIMO nao cabe na folga anterior";

    public const string PlantaUnica = "SEM LINHAS — só uma planta elegível tem capacidade para este produto";
    public const string PlantaEscolhida = "planta escolhida entre {0} elegíveis";

    public const string NaoAlocadoSemCapacidade = "SEM CAPACIDADE em todo o horizonte";
    public const string NaoAlocadoLoteMinimo = "LOTE MINIMO maior que a folga disponível";
    public const string NaoAlocadoPrioridade = "desprioritizado — capacidade foi para itens de prioridade maior";
}
