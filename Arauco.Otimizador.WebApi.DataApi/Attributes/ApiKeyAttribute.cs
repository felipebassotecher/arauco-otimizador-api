using Arauco.Otimizador.WebApi.DataApi.Filter;
using Microsoft.AspNetCore.Mvc;

namespace Arauco.Otimizador.WebApi.DataApi.Attributes;

public class ApiKeyAttribute : ServiceFilterAttribute
{
    public ApiKeyAttribute() : base(typeof(ApiKeyAuthorizationFilter))
    {
    }
}
