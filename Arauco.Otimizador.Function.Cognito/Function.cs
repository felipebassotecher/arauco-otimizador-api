using Amazon.Lambda.Core;
using Arauco.Otimizador.Aws.Shared;
using Arauco.Otimizador.Common.Domain.Enums;
using Arauco.Otimizador.Common.Domain.Session;
using Arauco.Otimizador.Data.Dynamo;
using Arauco.Otimizador.Data.MySql;
using Arauco.Otimizador.Function.Base;
using Arauco.Otimizador.Service.ContaService;
using System.Text.Json.Nodes;
using Techer.Common.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Arauco.Otimizador.Function.Cognito;

public class Function : BaseFunction
{
    private static readonly KeyValueRepository keyValueRepository = new();
    private static readonly AppSessionManager sessionManager = new(keyValueRepository);

    public async Task<object> FunctionHandler(JsonObject input, ILambdaContext context)
    {
        //context.Logger.LogLine(input.ToString());

        var data = ReadData(input);

        if (data != null)        
        {
            context.Logger.LogLine(JsonHelper.Serialize(data));

            // App
            var clientId = GetClientId(data.PoolId);

            if (clientId != data.ClientId)
                throw new Exception("INVALID_APP");

            // Session
            var sessionId = await CreateSession(data.Username);

            Console.WriteLine($"Session = {sessionId}");

            // Output
            WriteOutput(input, sessionId);
        }
        //context.Logger.LogLine(input.ToString());
        return input;
    }

    private static AuthData? ReadData(JsonObject input)
    {
        if (!input.TryGetPropertyValue("callerContext", out JsonNode? callerContextNode) || callerContextNode == null)
            return null;

        var callerContext = callerContextNode.AsObject();

        if (!callerContext.TryGetPropertyValue("clientId", out JsonNode? clientIdNode) || clientIdNode == null)
            return null;

        if (!input.TryGetPropertyValue("userName", out JsonNode? userNameNode) || userNameNode == null)
            return null;

        if (!input.TryGetPropertyValue("userPoolId", out JsonNode? poolNode) || poolNode == null)
            return null;

        return new AuthData(
            poolNode.GetValue<string>(),
            clientIdNode.GetValue<string>(),
            userNameNode.GetValue<string>());
    }

    private static void WriteOutput(JsonObject json, string sessionId)
    {
        if (json.ContainsKey("response"))
            json.Remove("response");

        var claims = new JsonObject();
        claims.Add("sid", sessionId);

        var claimsToAddOrOverride = new JsonObject();
        claimsToAddOrOverride.Add("claimsToAddOrOverride", claims);

        var response = new JsonObject();
        response.Add("claimsOverrideDetails", claimsToAddOrOverride);

        json.Add("response", response);
    }

    private static async Task<string> CreateSession(string userId)
    {
        var dbContext = SeniorDbContext.Create();
        var seniorUnitOfWork = new SeniorUnitOfWork(dbContext);

        var contaService = new ContaService(seniorUnitOfWork, environmentVariables);

        var session = await contaService.CriarSessaoAsync(Int32.Parse(userId));

        await sessionManager.AddAsync(
            userId,
            session);

        return session.SessionId;
    }

    private static string GetClientId(string poolId)
    {
        var cognito = Cognitos.App.Get(environmentVariables.GetEnvironmentEnum());
        
        if (cognito.PoolId != poolId)
        {
            return null;
        }

        return cognito.AppClientId;
    }

    private class AuthData
    {
        public string PoolId { get; set; }
        public string ClientId { get; private set; }
        public string Username { get; private set; }

        public AuthData(string poolId, string clientId, string username)
        {
            PoolId = poolId;
            ClientId = clientId;
            Username = username;
        }
    }

}
