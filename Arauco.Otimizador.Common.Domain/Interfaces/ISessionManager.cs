using Arauco.Otimizador.Common.Domain.Session;

namespace Arauco.Otimizador.Common.Domain.Interfaces
{
    public interface ISessionManager<S> where S : BaseSessionModel
    {
        Task AddAsync(string userId, S sessionModel);
        Task DeleteAsync(string userId);
        Task<S> GetAsync(string userId, string sessionId);
    }
}