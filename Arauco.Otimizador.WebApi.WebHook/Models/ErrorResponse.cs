namespace Arauco.Otimizador.WebApi.Flow.Models;

public class ErrorResponse : DataExchangeResponse
{
    public ErrorResponse(string screen, string errorMessage)
    {
        this.Screen = screen;
        this.Data = new 
        {
            error_message = errorMessage
        };
    }
}
