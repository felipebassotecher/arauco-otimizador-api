namespace Arauco.Otimizador.Service.OtimizadorService.Modelo;

public static class Greedy
{
    public static int TetoCarretas(double volumeCliente, Config config) =>
        Math.Clamp(
            (int)Math.Ceiling(volumeCliente / config.Carreta.MaximoM3),
            1, config.Carreta.MaximoCarretasPorEmbarque);
}
