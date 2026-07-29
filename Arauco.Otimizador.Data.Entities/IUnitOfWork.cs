using Techer.Common.Domain.Interfaces;

namespace Arauco.Otimizador.Data.Entities;

public interface IUnitOfWork
{
    // Cartao
    IGenericRepository<Cartao.Cartao> CartaoRepository { get; }

    Task SaveAsync();
}
