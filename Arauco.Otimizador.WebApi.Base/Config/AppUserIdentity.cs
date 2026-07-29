using Arauco.Otimizador.Common.Domain.Enums;
using Arauco.Otimizador.Common.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Arauco.Otimizador.WebApi.Base.Config;

public class AppUserIdentity : IUserIdentity
{
    private const string CLAIM_SESSION_ID = "sid";
    private const string CLAIM_USERNAME = "cognito:username";

    public string SessionId { get; set; }
    public string UserId { get; set; }

    public AppUserIdentity(string sessionId, string userId)
    {
        this.SessionId = sessionId;
        this.UserId = userId;
    }

    public AppUserIdentity(IHttpContextAccessor contextAccessor)
    {
        var httpContext = contextAccessor.HttpContext;

        if (httpContext != null)
        {
            this.SessionId = GetClaimValue(httpContext, CLAIM_SESSION_ID);
            this.UserId = GetClaimValue(httpContext, CLAIM_USERNAME);
        }
    }

    private static string GetClaimValue(HttpContext httpContext, string name)
    {
        var claim = httpContext.User?.Claims?.FirstOrDefault(x => x.Type == name);

        return claim?.Value;
    }
}
