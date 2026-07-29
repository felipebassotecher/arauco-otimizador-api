using Techer.Common.Domain.Enums;
using Techer.Common.Domain.Interfaces;
using Techer.Common.Extensions;

namespace Arauco.Otimizador.Function.Base.Providers;

public class FunctionEnvironmentVariables : IEnvironmentVariables
{
    private EnvironmentEnum env;

    public FunctionEnvironmentVariables()
    {
        var text = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLower();

        this.env = text switch
        {
            "local" => EnvironmentEnum.Dev,
            "development" => EnvironmentEnum.Dev,
            "testing" => EnvironmentEnum.Test,
            "production" => EnvironmentEnum.Prod,
            _ => throw new Exception("Environment inválido.")
        };
    }

    public FunctionEnvironmentVariables(EnvironmentEnum env)
    {
        this.env = env;
    }

    public FunctionEnvironmentVariables(string env)
    {
        env = env.Trim().ToLower();

        this.env = env switch
        {

            "dev" => EnvironmentEnum.Dev,
            "development" => EnvironmentEnum.Dev,
            "test" => EnvironmentEnum.Test,
            "testing" => EnvironmentEnum.Test,
            "prod" => EnvironmentEnum.Prod,
            "production" => EnvironmentEnum.Prod,
            _ => throw new Exception("Environment inválido.")
        };
    }

    public EnvironmentEnum GetEnvironmentEnum()
    {
        return this.env;
    }

    public string GetEnvironment()
    {
        return GetEnvironmentEnum().GetEnumMemberValue();
    }

    public bool IsProduction()
    {
        return this.env == EnvironmentEnum.Prod;
    }

    public bool IsDevelopment()
    {
        return this.env == EnvironmentEnum.Dev;
    }

    public bool IsLocal()
    {
        return System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLower() == "local";
    }

    public bool IsTesting()
    {
        return this.env == EnvironmentEnum.Test;
    }

    public string this[string key]
    {
        get
        {
            return null;
        }
    }
}
