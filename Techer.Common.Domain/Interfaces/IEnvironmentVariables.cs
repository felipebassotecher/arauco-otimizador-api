using Techer.Common.Domain.Enums;

namespace Techer.Common.Domain.Interfaces;

public interface IEnvironmentVariables
{
    bool IsLocal();
    bool IsDevelopment();
    bool IsProduction();
    bool IsTesting();
    string GetEnvironment();
    EnvironmentEnum GetEnvironmentEnum();

    string this[string key]
    {
        get;
    }
}
