using Techer.Common.Domain.Interfaces;

namespace Arauco.Otimizador.Common.Domain.Session
{
    public abstract class BaseSessionModel : ISessionModel
    {
        public string SessionId { get; set; }
    }
}
