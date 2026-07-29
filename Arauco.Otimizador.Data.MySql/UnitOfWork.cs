using Arauco.Otimizador.Data.Entities;
using Techer.Common.Domain.Interfaces;
using Techer.Data.MySql;

namespace Arauco.Otimizador.Data.MySql;

public class UnitOfWork : IUnitOfWork
{
    private readonly DbContext context;

    public UnitOfWork(DbContext context)
    {
        this.context = context;
    }

    // Cartao
    private IGenericRepository<Entities.Cartao.Cartao> cartaoRepository;
    public IGenericRepository<Entities.Cartao.Cartao> CartaoRepository
    {
        get
        {
            return this.cartaoRepository ??= new GenericRepository<DbContext, Entities.Cartao.Cartao>(context);
        }
    }

    // Cenario
    private IGenericRepository<Entities.Cenario.Cenario> cenarioRepository;
    public IGenericRepository<Entities.Cenario.Cenario> CenarioRepository
    {
        get
        {
            return this.cenarioRepository ??= new GenericRepository<DbContext, Entities.Cenario.Cenario>(context);
        }
    }

    private IGenericRepository<Entities.Cenario.CenarioParametro> cenarioParametroRepository;
    public IGenericRepository<Entities.Cenario.CenarioParametro> CenarioParametroRepository
    {
        get
        {
            return this.cenarioParametroRepository ??= new GenericRepository<DbContext, Entities.Cenario.CenarioParametro>(context);
        }
    }

    // Parametro
    private IGenericRepository<Entities.Parametro.Parametro> parametroRepository;
    public IGenericRepository<Entities.Parametro.Parametro> ParametroRepository
    {
        get
        {
            return this.parametroRepository ??= new GenericRepository<DbContext, Entities.Parametro.Parametro>(context);
        }
    }

    private IGenericRepository<Entities.Parametro.ParametroValor> parametroValorRepository;
    public IGenericRepository<Entities.Parametro.ParametroValor> ParametroValorRepository
    {
        get
        {
            return this.parametroValorRepository ??= new GenericRepository<DbContext, Entities.Parametro.ParametroValor>(context);
        }
    }

    // Demanda
    private IGenericRepository<Entities.Demanda.Demanda> demandaRepository;
    public IGenericRepository<Entities.Demanda.Demanda> DemandaRepository
    {
        get
        {
            return this.demandaRepository ??= new GenericRepository<DbContext, Entities.Demanda.Demanda>(context);
        }
    }

    // Pedido
    private IGenericRepository<Entities.Pedido.Pedido> pedidoRepository;
    public IGenericRepository<Entities.Pedido.Pedido> PedidoRepository
    {
        get
        {
            return this.pedidoRepository ??= new GenericRepository<DbContext, Entities.Pedido.Pedido>(context);
        }
    }

    public async Task SaveAsync()
    {
        await context.SaveChangesAsync();
    }

}
