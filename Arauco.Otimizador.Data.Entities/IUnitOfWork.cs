using Techer.Common.Domain.Interfaces;

namespace Arauco.Otimizador.Data.Entities;

public interface IUnitOfWork
{
    // Cartao
    IGenericRepository<Cartao.Cartao> CartaoRepository { get; }

    // Cenario
    IGenericRepository<Cenario.Cenario> CenarioRepository { get; }
    IGenericRepository<Cenario.CenarioArquivo> CenarioArquivoRepository { get; }

    // Demanda
    IGenericRepository<Demanda.Demanda> DemandaRepository { get; }

    // Pedido
    IGenericRepository<Pedido.Pedido> PedidoRepository { get; }

    // Setup
    IGenericRepository<Setup.Setup> SetupRepository { get; }
    IGenericRepository<Setup.SetupOrdemImportancia> SetupOrdemImportanciaRepository { get; }

    // Otimizador
    IGenericRepository<Otimizador.CenarioOtimizacaoResultado> CenarioOtimizacaoResultadoRepository { get; }
    IGenericRepository<Otimizador.PedidoOtimizado> PedidoOtimizadoRepository { get; }
    IGenericRepository<Otimizador.PedidoOtimizadoNaoAlocado> PedidoOtimizadoNaoAlocadoRepository { get; }
    IGenericRepository<Otimizador.PedidoOtimizadoMotivo> PedidoOtimizadoMotivoRepository { get; }

    // Dataset (master data consumida pelo motor de otimização — ver Data.Entities/Dataset)
    IGenericRepository<Dataset.Centro> CentroRepository { get; }
    IGenericRepository<Dataset.Produto> ProdutoRepository { get; }
    IGenericRepository<Dataset.LinhaProduto> LinhaProdutoRepository { get; }
    IGenericRepository<Dataset.Elegibilidade> ElegibilidadeRepository { get; }
    IGenericRepository<Dataset.Capacidade> CapacidadeRepository { get; }
    IGenericRepository<Dataset.Carteira> CarteiraRepository { get; }

    Task SaveAsync();
}