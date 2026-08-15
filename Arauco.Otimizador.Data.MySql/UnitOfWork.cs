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

    // Setup
    private IGenericRepository<Entities.Setup.Setup> setupRepository;
    public IGenericRepository<Entities.Setup.Setup> SetupRepository
    {
        get
        {
            return this.setupRepository ??= new GenericRepository<DbContext, Entities.Setup.Setup>(context);
        }
    }

    private IGenericRepository<Entities.Setup.SetupOrdemImportancia> setupOrdemImportanciaRepository;
    public IGenericRepository<Entities.Setup.SetupOrdemImportancia> SetupOrdemImportanciaRepository
    {
        get
        {
            return this.setupOrdemImportanciaRepository ??= new GenericRepository<DbContext, Entities.Setup.SetupOrdemImportancia>(context);
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

    private IGenericRepository<Entities.Otimizador.PedidoOtimizado> pedidoOtimizadoRepository;
    public IGenericRepository<Entities.Otimizador.PedidoOtimizado> PedidoOtimizadoRepository
    {
        get
        {
            return this.pedidoOtimizadoRepository ??= new GenericRepository<DbContext, Entities.Otimizador.PedidoOtimizado>(context);
        }
    }

    private IGenericRepository<Entities.Otimizador.PedidoOtimizadoNaoAlocado> pedidoOtimizadoNaoAlocadoRepository;
    public IGenericRepository<Entities.Otimizador.PedidoOtimizadoNaoAlocado> PedidoOtimizadoNaoAlocadoRepository
    {
        get
        {
            return this.pedidoOtimizadoNaoAlocadoRepository ??= new GenericRepository<DbContext, Entities.Otimizador.PedidoOtimizadoNaoAlocado>(context);
        }
    }

    // Dataset (master data consumida pelo motor de otimização)
    private IGenericRepository<Entities.Dataset.Centro> centroRepository;
    public IGenericRepository<Entities.Dataset.Centro> CentroRepository
    {
        get
        {
            return this.centroRepository ??= new GenericRepository<DbContext, Entities.Dataset.Centro>(context);
        }
    }

    private IGenericRepository<Entities.Dataset.Produto> produtoRepository;
    public IGenericRepository<Entities.Dataset.Produto> ProdutoRepository
    {
        get
        {
            return this.produtoRepository ??= new GenericRepository<DbContext, Entities.Dataset.Produto>(context);
        }
    }

    private IGenericRepository<Entities.Dataset.Elegibilidade> elegibilidadeRepository;
    public IGenericRepository<Entities.Dataset.Elegibilidade> ElegibilidadeRepository
    {
        get
        {
            return this.elegibilidadeRepository ??= new GenericRepository<DbContext, Entities.Dataset.Elegibilidade>(context);
        }
    }

    private IGenericRepository<Entities.Dataset.Capacidade> capacidadeRepository;
    public IGenericRepository<Entities.Dataset.Capacidade> CapacidadeRepository
    {
        get
        {
            return this.capacidadeRepository ??= new GenericRepository<DbContext, Entities.Dataset.Capacidade>(context);
        }
    }

    private IGenericRepository<Entities.Dataset.Carteira> carteiraRepository;
    public IGenericRepository<Entities.Dataset.Carteira> CarteiraRepository
    {
        get
        {
            return this.carteiraRepository ??= new GenericRepository<DbContext, Entities.Dataset.Carteira>(context);
        }
    }

    public async Task SaveAsync()
    {
        await context.SaveChangesAsync();
    }

}