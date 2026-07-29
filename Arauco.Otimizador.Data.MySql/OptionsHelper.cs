using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Arauco.Otimizador.Data.MySql
{
    public static class OptionsHelper
    {
        public const string ConnectionStringName = "DefaultConnection";

        public static string GetConnectionString(IConfiguration config)
        {
            var connString = config.GetConnectionString(ConnectionStringName);

            if (string.IsNullOrWhiteSpace(connString))
                throw new InvalidOperationException(
                    $"Connection string '{ConnectionStringName}' ausente. " +
                    $"Configure 'ConnectionStrings:{ConnectionStringName}' no appsettings.json.");

            return connString;
        }

        public static DbContextOptionsBuilder UseMySqlLocal(this DbContextOptionsBuilder options, IConfiguration config)
        {
            var connString = GetConnectionString(config);
            return options.UseMySql(connString, ServerVersion.AutoDetect(connString));
        }
    }
}