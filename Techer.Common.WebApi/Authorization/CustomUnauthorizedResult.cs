using Microsoft.AspNetCore.Mvc;

namespace Techer.Common.WebApi.Authorization
{
    public class CustomUnauthorizedResult : JsonResult
    {
        public CustomUnauthorizedResult(string message, int statusCode) : base(new CustomError(message))
        {
            StatusCode = statusCode;
        }
    }
}
