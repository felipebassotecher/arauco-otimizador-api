namespace Techer.Common.WebApi.Authorization
{
    public class CustomError
    {
        public string Message { get; }

        public CustomError(string message)
        {
            Message = message;
        }
    }
}
