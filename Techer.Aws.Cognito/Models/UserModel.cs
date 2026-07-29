namespace Techer.Aws.Cognito.Models
{
    public class UserModel
    {
        public string Username { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public bool Enabled { get; set; }
        public Dictionary<string, string> Attributes { get; set; } = new();
    }
}
