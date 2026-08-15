using Arauco.Otimizador.Common.Domain.Enums.Setup;

namespace Arauco.Otimizador.Common.Domain.Models.Setup;

public class SetupOrdemImportanciaRequest
{
    public CriterioOrdemEnum Criterio { get; set; }
    public int Ordem { get; set; }
}
