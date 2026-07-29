using Arauco.Otimizador.Aws.Shared;
using Arauco.Otimizador.Common.Domain.Enums;
using Arauco.Otimizador.Common.Domain.Models;
using Arauco.Otimizador.Common.Domain.Services.Auth;
using Arauco.Otimizador.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Techer.Aws.Cognito;
using Techer.Common.Domain.Interfaces;

namespace Arauco.Otimizador.Service.AuthService
{
    public class EntraService : IEntraService
    {
        private const string PASSWORD = "b5f779c8-809f-11ee-b962-0242ac120002";

        private const string CLAIM_EVENT_ID = "oid";
        private const string CLAIM_EMAIL = "email";
        private const string CLAIM_NAME = "name";
        private const string CLAIM_UNIQUE_NAME = "unique_name";

        private readonly ISeniorUnitOfWork seniorUnitOfWork;
        private readonly CognitoHelper cognitoHelper;

        public EntraService(ISeniorUnitOfWork seniorUnitOfWork, IEnvironmentVariables environmentVariables)
        {
            this.seniorUnitOfWork = seniorUnitOfWork;
            this.cognitoHelper = new CognitoHelper(environmentVariables, Cognitos.App);
        }

        public async Task<CognitoModel> AutenticarAsync(string accessToken, EntraConfigurationModel entraConfig)
        {
            var claims = JwtUtil.GetClaims(accessToken);

            if (!claims.Any(c => c.Key == CLAIM_EMAIL) && !claims.Any(c => c.Key == CLAIM_UNIQUE_NAME))
                throw new Exception("Token inválido.");

            if (!claims.Any(c => c.Key == CLAIM_NAME))
                throw new Exception("Token inválido.");

            if (!claims.Any(c => c.Key == CLAIM_EVENT_ID))
                throw new Exception("Token inválido.");

            string email = claims.Any(c => c.Key == CLAIM_EMAIL) ? claims.First(c => c.Key == CLAIM_EMAIL).Value : claims.First(c => c.Key == CLAIM_UNIQUE_NAME).Value;
            string sessionId = claims.First(c => c.Key == CLAIM_EVENT_ID).Value;
            string name = claims.First(c => c.Key == CLAIM_NAME).Value;

            var usuario = await seniorUnitOfWork
                .ColaboradorRepository
                .Where(u => u.EmailComercial == email && u.Ativo)
                .Select(u => new
                {
                    u.ColaboradorId
                }).FirstOrDefaultAsync();

            if (usuario == null)
                throw new Exception("Usuário não localizado.");

            // Autenticacao Cognito
            var res = await cognitoHelper.AuthenticateAsync(
                usuario.ColaboradorId.ToString(),
                $"{usuario.ColaboradorId}#{PASSWORD}");

            return new CognitoModel
            {
                AccessToken = res.AccessToken,
                ExpiresIn = res.ExpiresIn,
                IdToken = res.IdToken,
                RefreshToken = res.RefreshToken
            };
        }
    }
}