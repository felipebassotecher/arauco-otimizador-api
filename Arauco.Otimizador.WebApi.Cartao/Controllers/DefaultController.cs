using Arauco.Otimizador.WebApi.Base.Controller;
using Microsoft.AspNetCore.Mvc;

namespace Arauco.Otimizador.WebApi.Controllers;

[Route("")]
public class DefaultController : BaseController
{
    public DefaultController(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        
    }

    [HttpGet("check")]
    public async Task<OkResult> CheckAsync()
    {
        return Ok();
    }
}
