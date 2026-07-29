using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Arauco.Otimizador.DataApi.Filters
{
    public class ApiKeyAuthorizationFilter : IAuthorizationFilter
    {
        private const string AUTH_HEADER = "Authorization";
        private const string API_KEY = "074c2037d8cb453b8d3fae06d51c4e09";

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var isValid = context.HttpContext.Request.Headers.ContainsKey(AUTH_HEADER)
                && context.HttpContext.Request.Headers[AUTH_HEADER].ToString().Equals(API_KEY, StringComparison.OrdinalIgnoreCase);

            if (!isValid)
            {
                context.Result = new UnauthorizedResult();
            }
        }
    }
}
