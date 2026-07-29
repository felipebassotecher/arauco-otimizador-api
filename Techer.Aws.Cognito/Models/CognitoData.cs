namespace Techer.Aws.Cognito.Models
{
    public class CognitoData
    {
        public string PoolId { get; set; }
        public string Authority { get; set; }

        public string AppClientId { get; set; }

        public CognitoData(string userPoolId, string appClientId)
        {
            Set(userPoolId, appClientId);
        }

        public void Set(string userPoolId, string appClientId)
        {
            PoolId = userPoolId;
            AppClientId = appClientId;
            Authority = $"https://cognito-idp.sa-east-1.amazonaws.com/{userPoolId}";
        }
    }
}
