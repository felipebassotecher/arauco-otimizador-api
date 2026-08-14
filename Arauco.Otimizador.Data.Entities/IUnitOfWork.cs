using Techer.Common.Domain.Interfaces;

namespace Arauco.Otimizador.Data.Entities;

public interface IUnitOfWork
{
    // Cartao
    IGenericRepository<Cartao.Cartao> CartaoRepository { get; }

    // Cenario
    IGenericRepository<Cenario.Cenario> CenarioRepository { get; }
    IGenericRepository<Cenario.CenarioCriterio> CenarioCriterioRepository { get; }
    IGenericRepository<Cenario.CenarioArquivo> CenarioArquivoRepository { get; }

    // Demanda
    IGenericRepository<Demanda.Demanda> DemandaRepository { get; }

    // Pedido
    IGenericRepository<Pedido.Pedido> PedidoRepository { get; }

    // Otimizador
    IGenericRepository<Otimizador.CenarioOtimizacaoResultado> CenarioOtimizacaoResultadoRepository { get; }
    IGenericRepository<Otimizador.OtimizacaoAlocacao> OtimizacaoAlocacaoRepository { get; }
    IGenericRepository<Otimizador.OtimizacaoNaoAlocado> OtimizacaoNaoAlocadoRepository { get; }
    IGenericRepository<Otimizador.OtimizacaoEmbarque> OtimizacaoEmbarqueRepository { get; }
    IGenericRepository<Otimizador.OtimizacaoOcupacao> OtimizacaoOcupacaoRepository { get; }
    IGenericRepository<Otimizador.OtimizacaoCriterio> OtimizacaoCriterioRepository { get; }

    // OtimizadorV2
    IGenericRepository<OtimizadorV2.CenarioOtimizacaoV2Resultado> CenarioOtimizacaoV2ResultadoRepository { get; }
    IGenericRepository<OtimizadorV2.PedidoV2> PedidoV2Repository { get; }
    IGenericRepository<OtimizadorV2.PedidoV2NaoAlocado> PedidoV2NaoAlocadoRepository { get; }

    Task SaveAsync();
}