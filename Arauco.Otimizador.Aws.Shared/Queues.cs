using Techer.Aws.Shared;

namespace Arauco.Otimizador.Aws.Shared
{
    public static class Queues
    {
        public static AwsUrlResource Email = new AwsUrlResource
        {
            Region = Regions.SA_EAST_1,
            Production = "https://sqs.sa-east-1.amazonaws.com/##########/email-outbound",
            Testing = "",
            Development = "https://sqs.sa-east-1.amazonaws.com/##########/email-outbound"
        };
    }
}
