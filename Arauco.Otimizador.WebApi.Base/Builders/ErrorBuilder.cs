using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.Net;
using Techer.Common.Domain.Exceptions;
using Techer.Common.Json;

namespace Arauco.Otimizador.WebApi.Base.Builders
{
    public static class ErrorBuilder
    {
        public static void Generate(IApplicationBuilder builder)
        {
            builder.Run(
            async context =>
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var corsService = (ICorsService)context.RequestServices.GetService(typeof(ICorsService));
                var corsPolicyProvider = (ICorsPolicyProvider)context.RequestServices.GetService(typeof(ICorsPolicyProvider));
                var corsPolicy = await corsPolicyProvider.GetPolicyAsync(context, "Default");

                var ex = context.Features.Get<IExceptionHandlerFeature>();
                if (ex != null)
                {
                    // Customized error code
                    object error = null;
                    if (ex.Error is ModelValidationException)
                    {
                        error = new
                        {
                            message = ex.Error.Message,
                            failures = (ex.Error as ModelValidationException).Failures,
                            trace = ex.Error.StackTrace
                        };
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                    else if (ex.Error is ApiException)
                    {
                        error = new
                        {
                            message = ex.Error.InnerException != null ? ex.Error.InnerException.Message : ex.Error.Message,
                            trace = ex.Error.StackTrace,
                            knownError = true
                        };

                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                    else
                    {
                        switch (ex.Error)
                        {
                            case InvalidSessionException:
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                break;
                            case NotFoundException:
                                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                                break;
                            case SimultaneousAccessException:
                                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                                break;
                        }

                        error = new
                        {
                            message = ex.Error.InnerException != null ? ex.Error.InnerException.Message : ex.Error.Message,
                            trace = ex.Error.StackTrace
                        };
                    }

                    corsService.ApplyResult(
                        corsService.EvaluatePolicy(context, corsPolicy),
                        context.Response);

                    await context.Response.WriteAsync(JsonHelper.Serialize(error));
                }
            });
        }
    }
}
