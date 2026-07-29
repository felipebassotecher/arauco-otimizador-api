namespace Techer.Aws.Cognito.Models
{
    public class AuthenticateModel
    {
        public string AccessToken { get; set; }
        public string IdToken { get; set; }
        public int ExpiresIn { get; set; }
        public string RefreshToken { get; set; }
    }
}
