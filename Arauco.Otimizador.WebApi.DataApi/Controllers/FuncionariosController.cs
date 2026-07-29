using Arauco.Otimizador.Data.Entities;
using Arauco.Otimizador.WebApi.DataApi.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;

namespace Arauco.Otimizador.WebApi.DataApi.Controllers;

[ApiKey]
public class FuncionariosController : ODataController
{
    private readonly IUnitOfWork unitOfWork;

    public FuncionariosController(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    [EnableQuery]
    public async Task<IActionResult> Get()
    {
        var res = await unitOfWork
            .FuncionarioRepository
            .AsQueryable()
            .ToListAsync();

        return Ok(res);
    }

    [EnableQuery]
    public async Task<ActionResult> Get(string key)
    {
        var res = await unitOfWork
            .FuncionarioRepository
            .Where(f => f.FuncionarioId == key)
            .FirstOrDefaultAsync();

        return Ok(res);
    }
}