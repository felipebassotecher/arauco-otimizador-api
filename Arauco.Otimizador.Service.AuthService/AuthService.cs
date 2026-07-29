using Arauco.Otimizador.Common.Domain.Enums;
using Arauco.Otimizador.Common.Domain.Services.Auth;
using Arauco.Otimizador.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Techer.Common.Domain.Exceptions;
using Techer.Common.Domain.Interfaces;

namespace Arauco.Otimizador.Service.AuthService
{
    public class AuthService : CognitoService.CognitoService, IAuthService
    {
        private readonly ISeniorUnitOfWork seniorUnitOfWork;

        public AuthService(ISeniorUnitOfWork seniorUnitOfWork, IEnvironmentVariables environmentVariables): base(null, environmentVariables)
        {
            this.seniorUnitOfWork = seniorUnitOfWork;
        }

        public async Task RedefinirSenha(string email)
        {
            var usuario = await seniorUnitOfWork
                .ColaboradorRepository
                .Where(u => (u.EmailComercial == email || u.EmailParticular == email) && u.Ativo && !u.Excluido)
                .FirstOrDefaultAsync();

            if (usuario == null)
                throw new NotFoundException("Usuário não encontrado.");

            await ChangePasswordAsync(usuario, email);
        }
    }
}
