using Arauco.Otimizador.Common.Domain.Session;
using Microsoft.AspNetCore.Mvc;

namespace Arauco.Otimizador.WebApi.Base.Controller
{
    [ApiController]
    public abstract class BaseController : Microsoft.AspNetCore.Mvc.Controller
    {
        //protected readonly ISessionManager<AppSessionModel> sessionManager;
        //protected readonly IUserIdentity tokenData;

        public BaseController(IServiceProvider serviceProvider)
        {
            //sessionManager = serviceProvider.GetRequiredService<ISessionManager<AppSessionModel>>();
            //tokenData = serviceProvider.GetRequiredService<IUserIdentity>();
        }

        protected async Task<AppSessionModel> GetSessionAsync()
        {
            //var session = await sessionManager.GetAsync(
            //    tokenData.UserId,
            //    tokenData.SessionId);

            //if (session == null)
            //    throw new InvalidSessionException();

            //// Set current IP
            //session.CurrentIp = GetIpAddress();

            //return session;

            return null;
        }

        protected string GetIpAddress()
        {
            return Request.HttpContext.Connection.RemoteIpAddress.ToString();
        }
    }
}
