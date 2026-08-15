using Arauco.Otimizador.Common.Domain.Enums.Setup;

namespace Arauco.Otimizador.Data.Entities.Setup;

public class SetupOrdemImportancia
{
    public int Id { get; set; }
    public string SetupId { get; set; }
    public CriterioOrdemEnum CriterioEnum { get; set; }
    public int Ordem { get; set; }
    public string? Descricao { get; set; }
    public bool Ativo { get; set; }
}
