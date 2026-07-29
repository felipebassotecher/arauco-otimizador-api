using Arauco.Otimizador.Common.Domain.Models;
using Arauco.Otimizador.Common.Domain.Services.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Arauco.Otimizador.WebApi.AuthApi.Controllers;

[Route("[controller]")]
public class EntraController : ControllerBase
{
    private readonly IEntraService entraService;
    private readonly EntraConfigurationModel entraConfiguration;

    public EntraController(IEntraService entraService, IOptions<EntraConfigurationModel> entraConfigurationOptions)
    {
        this.entraService = entraService;
        this.entraConfiguration = entraConfigurationOptions.Value;
    }

    [HttpPost]
    public async Task<ActionResult<CognitoModel>> Autenticar([FromBody] Models.Entra.EntraRequest model)
    {
        try
        {
            var res = await entraService.AutenticarAsync(model.AccessToken, this.entraConfiguration);

            return Ok(res);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                error = ex.Message
            });
        }                    
    }
}