using Techer.Common.Domain.Enums;
using Techer.Common.Domain.Interfaces;

namespace Arauco.Otimizador.Common.Domain.Constants
{
    public static class AppDomains
    {
        public static string GetAuthDomain(IEnvironmentVariables env)
        {
            return env.GetEnvironmentEnum() switch
            {
                EnvironmentEnum.Dev => "https://dev.auth.arauco.app.br/",
                EnvironmentEnum.Test => "https://test.auth.arauco.app.br/",
                _ => "https://auth.arauco.app.br/",
            };
        }
    }
}
