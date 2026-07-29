using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Principal;
using System.Text;

namespace Arauco.Otimizador.Service.AuthService
{
    internal class JwtUtil
    {
        public static async Task<bool> IsValid(string jwtTokenToValidate, string tenantId, string clientId)
        {
            var openIdConnectWellKnownConfigUri = new Uri("https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration");

            try
            {
                var openIdConfigManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    openIdConnectWellKnownConfigUri.ToString(),
                    new OpenIdConnectConfigurationRetriever()
                );

                OpenIdConnectConfiguration openIdConfig = await openIdConfigManager.GetConfigurationAsync().ConfigureAwait(false);
                TokenValidationParameters validationParams = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ValidateTokenReplay = true,
                    ValidAudience = clientId,
                    ValidIssuer = $"https://sts.windows.net/{tenantId}/",
                    //ValidIssuer = openIdConfig.Issuer.Replace("{tenantid}", tenantId),
                    IssuerSigningKeys = openIdConfig.SigningKeys,
                };

                var jwtTokenHandler = new JwtSecurityTokenHandler();
                jwtTokenHandler.ValidateToken(jwtTokenToValidate, validationParams, out _);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static List<KeyValuePair<string, string>> GetClaims(string accessToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtDecoded = handler.ReadToken(accessToken) as JwtSecurityToken;

            return jwtDecoded
                .Claims
                .Select(c => KeyValuePair.Create(c.Type, c.Value))
                .ToList();
        }
    }
}
