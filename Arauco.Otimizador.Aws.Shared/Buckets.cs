using Techer.Aws.Shared;

namespace Arauco.Otimizador.Aws.Shared
{
    public static class Buckets
    {
        public static AwsStringResource Storage = new AwsStringResource
        {
            Region = Regions.SA_EAST_1,
            Development = "arauco-otimizador-dev",
            Testing = "arauco-otimizador-test",
            Production = "arauco-otimizador-prod"
        };

        public static AwsStringResource Temp = new AwsStringResource
        {
            Region = Regions.SA_EAST_1,
            Development = "arauco-otimizador-temp-dev",
            Testing = "arauco-otimizador-temp-test",
            Production = "arauco-otimizador-temp-prod"
        };
    }
}
