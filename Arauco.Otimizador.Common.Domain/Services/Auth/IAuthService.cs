using Arauco.Otimizador.Common.Domain.Enums;

namespace Arauco.Otimizador.Common.Domain.Services.Auth
{
    public interface IAuthService
    {
        Task RedefinirSenha(string email);
        //Task<PerfilModel> ObterPerfil();
    }
}
