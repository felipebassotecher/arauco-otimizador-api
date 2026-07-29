using Techer.Aws.Cognito.Models;
using Techer.Aws.Shared;

namespace Arauco.Otimizador.Aws.Shared;

public static class Cognitos
{
    public static AwsResource<CognitoData> App = new AwsResource<CognitoData>
    {
        Region = Regions.SA_EAST_1,
        Development = new CognitoData("sa-east-1_##########", "########################"),
        Testing = new CognitoData("sa-east-1_##########", "########################"),
        Production = new CognitoData("sa-east-1_##########", "########################"),
    };
}
