using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Arauco.Otimizador.WebApi.DataApi.Filter;

public class ApiKeyAuthorizationFilter : IAuthorizationFilter
{
    private const string AUTH_HEADER = "Authorization";
    private const string API_KEY = "c62fc5ca9b7a11ee8c900242ac120002";

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
