using Arauco.Otimizador.Common.Domain.Models.Demanda;
using Arauco.Otimizador.Common.Domain.Services.Demanda;
using Arauco.Otimizador.WebApi.Base.Controller;
using Microsoft.AspNetCore.Mvc;

namespace Arauco.Otimizador.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class DemandasController : BaseController
{
    private readonly IDemandaService demandaService;

    public DemandasController(IDemandaService demandaService, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        this.demandaService = demandaService;
    }

    [HttpGet("")]
    public async Task<List<DemandaResponse>> ListarAsync([FromQuery] string cenarioId)
    {
        return await demandaService.ListarAsync(cenarioId);
    }

    [HttpPost("upload")]
    public async Task<List<DemandaResponse>> UploadAsync([FromBody] DemandaUploadRequest model)
    {
        return await demandaService.UploadAsync(model);
    }
}