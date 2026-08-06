namespace Arauco.Otimizador.Common.Domain.Models.Conta;

// Retornado por GET /conta/profile (spec §3.15).
public class PerfilResponse
{
    public string ColaboradorId { get; set; }
    public string Nome { get; set; }
    public string Email { get; set; }
}