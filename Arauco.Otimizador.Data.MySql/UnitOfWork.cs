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

    private IGenericRepository<Entities.Cenario.CenarioCriterio> cenarioCriterioRepository;
    public IGenericRepository<Entities.Cenario.CenarioCriterio> CenarioCriterioRepository
    {
        get
        {
            return this.cenarioCriterioRepository ??= new GenericRepository<DbContext, Entities.Cenario.CenarioCriterio>(context);
        }
    }

    private IGenericRepository<Entities.Cenario.CenarioArquivo> cenarioArquivoRepository;
    public IGenericRepository<Entities.Cenario.CenarioArquivo> CenarioArquivoRepository
    {
        get
        {
            return this.cenarioArquivoRepository ??= new GenericRepository<DbContext, Entities.Cenario.CenarioArquivo>(context);
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

    // Otimizador
    private IGenericRepository<Entities.Otimizador.CenarioOtimizacaoResultado> cenarioOtimizacaoResultadoRepository;
    public IGenericRepository<Entities.Otimizador.CenarioOtimizacaoResultado> CenarioOtimizacaoResultadoRepository
    {
        get
        {
            return this.cenarioOtimizacaoResultadoRepository ??= new GenericRepository<DbContext, Entities.Otimizador.CenarioOtimizacaoResultado>(context);
        }
    }

    private IGenericRepository<Entities.Otimizador.OtimizacaoAlocacao> otimizacaoAlocacaoRepository;
    public IGenericRepository<Entities.Otimizador.OtimizacaoAlocacao> OtimizacaoAlocacaoRepository
    {
        get
        {
            return this.otimizacaoAlocacaoRepository ??= new GenericRepository<DbContext, Entities.Otimizador.OtimizacaoAlocacao>(context);
        }
    }

    private IGenericRepository<Entities.Otimizador.OtimizacaoNaoAlocado> otimizacaoNaoAlocadoRepository;
    public IGenericRepository<Entities.Otimizador.OtimizacaoNaoAlocado> OtimizacaoNaoAlocadoRepository
    {
        get
        {
            return this.otimizacaoNaoAlocadoRepository ??= new GenericRepository<DbContext, Entities.Otimizador.OtimizacaoNaoAlocado>(context);
        }
    }

    private IGenericRepository<Entities.Otimizador.OtimizacaoEmbarque> otimizacaoEmbarqueRepository;
    public IGenericRepository<Entities.Otimizador.OtimizacaoEmbarque> OtimizacaoEmbarqueRepository
    {
        get
        {
            return this.otimizacaoEmbarqueRepository ??= new GenericRepository<DbContext, Entities.Otimizador.OtimizacaoEmbarque>(context);
        }
    }

    private IGenericRepository<Entities.Otimizador.OtimizacaoOcupacao> otimizacaoOcupacaoRepository;
    public IGenericRepository<Entities.Otimizador.OtimizacaoOcupacao> OtimizacaoOcupacaoRepository
    {
        get
        {
            return this.otimizacaoOcupacaoRepository ??= new GenericRepository<DbContext, Entities.Otimizador.OtimizacaoOcupacao>(context);
        }
    }

    private IGenericRepository<Entities.Otimizador.OtimizacaoCriterio> otimizacaoCriterioRepository;
    public IGenericRepository<Entities.Otimizador.OtimizacaoCriterio> OtimizacaoCriterioRepository
    {
        get
        {
            return this.otimizacaoCriterioRepository ??= new GenericRepository<DbContext, Entities.Otimizador.OtimizacaoCriterio>(context);
        }
    }

    public async Task SaveAsync()
    {
        await context.SaveChangesAsync();
    }

}