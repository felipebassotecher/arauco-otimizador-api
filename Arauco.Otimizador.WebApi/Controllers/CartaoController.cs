using Arauco.Otimizador.Common.Domain.Models.Cartao;
using Arauco.Otimizador.Common.Domain.Services.Cartao;
using Arauco.Otimizador.WebApi.Base.Controller;
using Microsoft.AspNetCore.Mvc;

namespace Arauco.Otimizador.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class CartaoController : BaseController
{
    private readonly ICartaoService cartaoService;
    public CartaoController(ICartaoService cartaoService, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        this.cartaoService = cartaoService;
    }

    [HttpGet("todos")]
    public async Task<List<CartaoListaModel>> ListarAsync()
    {
        return await cartaoService.ListarAsync(await GetSessionAsync());
    }

    [HttpGet("{cartaoId}")]
    public async Task<CartaoDetalheModel> ObterAsync(string cartaoId)
    {
        return await cartaoService.ObterAsync(cartaoId, await GetSessionAsync());
    }

    [HttpPost("novo")]
    public async Task<JsonResult> CriarAsync([FromBody] CartaoNovoModel cartao)
    {
        var cartaoId = await cartaoService.NovoAsync(cartao, await GetSessionAsync());

        return Json(cartaoId);
    }
}
