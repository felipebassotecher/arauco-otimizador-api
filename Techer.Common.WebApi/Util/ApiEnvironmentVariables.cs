using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Techer.Common.Domain.Enums;
using Techer.Common.Domain.Interfaces;
using Techer.Common.Extensions;

namespace Techer.Common.WebApi.Util
{
    public class ApiEnvironmentVariables : IEnvironmentVariables
    {
        private const string ENV_TESTING = "Testing";
        private const string ENV_LOCAL = "Local";

        private readonly IConfiguration config;
        private readonly IWebHostEnvironment env;

        public ApiEnvironmentVariables(IConfiguration config, IWebHostEnvironment env)
        {
            this.config = config;
            this.env = env;
        }

        public bool IsLocal()
        {
            return env.IsEnvironment(ENV_LOCAL);
        }

        public bool IsDevelopment()
        {
            return env.IsDevelopment();
        }

        public bool IsProduction()
        {
            return env.IsProduction();
        }

        public bool IsTesting()
        {
            return env.IsEnvironment(ENV_TESTING);
        }

        public EnvironmentEnum GetEnvironmentEnum()
        {
            return GetEnvironmentEnum(env.EnvironmentName);
        }

        public string GetEnvironment()
        {
            return GetEnvironmentEnum().GetEnumMemberValue();
        }

        public string this[string key]
        {
            get
            {
                return config[key];
            }
        }

        public static EnvironmentEnum GetEnvironmentEnum(string environmentName)
        {
            var res = EnvironmentEnum.Dev;

            switch (environmentName.ToLower())
            {
                case "local":
                case "development":
                case "dev":
                    res = EnvironmentEnum.Dev;
                    break;

                case "testing":
                case "test":
                    res = EnvironmentEnum.Test;
                    break;

                case "production":
                case "prod":
                    res = EnvironmentEnum.Prod;
                    break;

                default:
                    throw new Exception("Environment inválido.");
            }

            return res;
        }

    }
}
