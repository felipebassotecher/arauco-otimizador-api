using Arauco.Otimizador.WebApi.Flow.Flow;
using Arauco.Otimizador.WebApi.Flow.Flow.Refeicao;
using Arauco.Otimizador.WebApi.Flow.Models;
using Arauco.Otimizador.WebApi.Flow.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techer.Common.Json;

namespace Arauco.Otimizador.WebApi.Flow.Controllers;

[ApiController]
[AllowAnonymous]
[Route("[controller]")]
public class RefeicaoController : ControllerBase
{
    private readonly CryptoService cryptoService;
    private readonly FlowHandler flowHandler;

    public RefeicaoController(CryptoService cryptoService, RefeicaoFlowHandler flowHandler)
    {
        this.cryptoService = cryptoService;
        this.flowHandler = flowHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Request(FlowEncryptedModel model)
    {
        var d = cryptoService.DecryptRequest(model);

        var payload = JsonHelper.Deserialize<DataExchangeRequest>(d.PlainText);

#if DEBUG
        Console.WriteLine("====== REQUEST ======");
        Console.WriteLine(JsonHelper.Serialize(payload));
#endif

        var res = await flowHandler.HandleAsync(payload);

        if (res == null)
            return StatusCode(500, "Erro");

        var jsonResult = JsonHelper.Serialize(res);

#if DEBUG
        Console.WriteLine("====== RESPONSE ======");
        Console.WriteLine(jsonResult);
#endif

        var encryptedResult = cryptoService.EncryptResponse(jsonResult, d.aesKey, d.iv);

        return StatusCode(200, encryptedResult);
    }
}
