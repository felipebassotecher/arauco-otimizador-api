using Arauco.Otimizador.Common.Domain.Services.Auth;
using Arauco.Otimizador.WebApi.AuthApi.Models.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Arauco.Otimizador.WebApi.AuthApi.Controllers
{
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService authService;

        public AuthController(IAuthService authService)
        {
            this.authService = authService;
        }

        [HttpPost("redefinir-senha")]
        public async Task RedefinirSenha([FromBody] RedefinirSenhaModel model)
        {
            await authService.RedefinirSenha(model.Email);
        }
    }
}
