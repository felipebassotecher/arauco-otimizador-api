namespace Arauco.Otimizador.Data.Entities.Cenario;

// Conteúdo original do CSV de demandas carregado para o cenário, mantido disponível para download
// (GET /cenarios/{id}/csv — spec §2.2/§3.4.3). Um por cenário (o upload é permitido apenas uma vez).
public class CenarioArquivo
{
    public string CenarioId { get; set; }
    public string Nome { get; set; }
    public string Conteudo { get; set; }
    public DateTime DataUpload { get; set; }
}