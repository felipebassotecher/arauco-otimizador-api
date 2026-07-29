using Arauco.Otimizador.Common.Domain.Models;

namespace Arauco.Otimizador.Common.Domain.Services.Auth;

public interface IEntraService
{
    Task<CognitoModel> AutenticarAsync(string accessToken, EntraConfigurationModel entraConfig);
}