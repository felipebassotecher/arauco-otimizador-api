namespace Techer.Common.Domain.Exceptions
{
    public class InvalidSessionException : Exception
    {
        public InvalidSessionException() : base("Sessão não encontrada ou inválida.")
        {
        }
    }
}
