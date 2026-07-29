namespace Arauco.Otimizador.Common.Domain.Models;

public class CognitoModel
{
    public string AccessToken { get; set; }
    public string IdToken { get; set; }
    public int ExpiresIn { get; set; }
    public string RefreshToken { get; set; }
}
