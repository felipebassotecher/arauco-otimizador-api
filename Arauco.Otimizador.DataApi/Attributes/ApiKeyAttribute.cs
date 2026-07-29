using Arauco.Otimizador.DataApi.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Arauco.Otimizador.DataApi.Attributes
{
    public class ApiKeyAttribute : ServiceFilterAttribute
    {
        public ApiKeyAttribute() : base(typeof(ApiKeyAuthorizationFilter))
        {
        }
    }
}
