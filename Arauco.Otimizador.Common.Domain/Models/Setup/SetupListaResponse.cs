namespace Arauco.Otimizador.Common.Domain.Models.Setup;

public class SetupListaResponse
{
    public string Id { get; set; }
    public string Nome { get; set; }
    public string? Descricao { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAlteracao { get; set; }
    public bool PossuiOrdemImportancia { get; set; }
}
