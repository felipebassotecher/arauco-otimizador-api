namespace Arauco.Otimizador.WebApi.Flow.Exceptions;

public class FlowEndpointException : Exception
{
    public int StatusCode { get; }

    public FlowEndpointException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}