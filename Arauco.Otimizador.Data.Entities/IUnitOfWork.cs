using Techer.Common.Domain.Interfaces;

namespace Arauco.Otimizador.Data.Entities;

public interface IUnitOfWork
{
    // Cartao
    IGenericRepository<Cartao.Cartao> CartaoRepository { get; }

    // Cenario
    IGenericRepository<Cenario.Cenario> CenarioRepository { get; }
    IGenericRepository<Cenario.CenarioParametro> CenarioParametroRepository { get; }

    // Parametro
    IGenericRepository<Parametro.Parametro> ParametroRepository { get; }
    IGenericRepository<Parametro.ParametroValor> ParametroValorRepository { get; }

    // Demanda
    IGenericRepository<Demanda.Demanda> DemandaRepository { get; }

    // Pedido
    IGenericRepository<Pedido.Pedido> PedidoRepository { get; }

    Task SaveAsync();
}
