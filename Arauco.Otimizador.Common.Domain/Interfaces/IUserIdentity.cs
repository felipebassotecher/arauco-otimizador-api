namespace Arauco.Otimizador.Common.Domain.Interfaces
{
    public interface IUserIdentity
    {
        string SessionId { get; }
        string UserId { get; }
    }
}
