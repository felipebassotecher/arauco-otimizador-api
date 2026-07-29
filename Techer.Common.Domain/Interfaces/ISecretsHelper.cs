namespace Techer.Common.Domain.Interfaces
{
    public interface ISecretsHelper
    {
        Task<string> GetSecretAsync(string secretName);
        string GetSecret(string secretName);
    }
}
