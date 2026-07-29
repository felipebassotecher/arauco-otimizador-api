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

    public async Task SaveAsync()
    {
        await context.SaveChangesAsync();
    }

}
