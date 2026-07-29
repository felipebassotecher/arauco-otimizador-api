using System.Runtime.Serialization;

namespace Techer.Common.Domain.Enums;

public enum EnvironmentEnum
{
    [EnumMember(Value = "dev")]
    Dev = 1,

    [EnumMember(Value = "test")]
    Test = 2,

    [EnumMember(Value = "prod")]
    Prod = 3
}
