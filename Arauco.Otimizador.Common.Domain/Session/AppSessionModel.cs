namespace Arauco.Otimizador.Common.Domain.Session;

public class AppSessionModel : BaseSessionModel
{
    public int ColaboradorId { get; set; }
    public string Nome { get; set; }
    public string Email { get; set; }
    public string CurrentIp { get; set; }
}
