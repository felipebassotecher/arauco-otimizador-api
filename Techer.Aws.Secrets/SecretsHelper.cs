using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Newtonsoft.Json;
using Techer.Common.Domain.Interfaces;
using Techer.Common.Json;

namespace Techer.Aws.Secrets;

public class SecretsHelper : ISecretsHelper
{
    private static readonly IAmazonSecretsManager client = new AmazonSecretsManagerClient(Amazon.RegionEndpoint.SAEast1);
    private static readonly Dictionary<string, string> cache = new();


    public async Task<string> GetSecretAsync(string secretName)
    {
        string secret = null;

        if (cache.ContainsKey(secretName))
        {
            secret = cache[secretName];
        }
        else
        {
            var request = new GetSecretValueRequest
            {
                SecretId = secretName,
                VersionStage = "AWSCURRENT"
            };

            var response = await client.GetSecretValueAsync(request);

            if (response.SecretString != null)
            {
                secret = response.SecretString;
            }
            else
            {
                using (var ms = new MemoryStream())
                using (var sr = new StreamReader(ms))
                {
                    var tmp = await sr.ReadToEndAsync();
                    secret = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(tmp));
                }
            }

            cache.Add(secretName, secret);
        }

        return secret;
    }

    public string GetSecret(string secretName)
    {
        string secret = null;

        if (cache.ContainsKey(secretName))
        {
            secret = cache[secretName];
        }
        else
        {
            var request = new GetSecretValueRequest
            {
                SecretId = secretName,
                VersionStage = "AWSCURRENT"
            };

            var response = client.GetSecretValueAsync(request).Result;

            if (response.SecretString != null)
            {
                secret = response.SecretString;
            }
            else
            {
                using (var ms = new MemoryStream())
                using (var sr = new StreamReader(ms))
                {
                    secret = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(sr.ReadToEnd()));
                }
            }

            cache.Add(secretName, secret);
        }

        return secret;
    }

    public async Task<string> CreateSecret(string name, string description, string engine, string host, int port, string dbName, string username, string password)
    {
        var model = new SecretModel
        {
            Engine = engine,
            Host = host,
            Port = port.ToString(),
            DbName = dbName,
            Username = username,
            Password = password
        };

        var res = await client.CreateSecretAsync(new CreateSecretRequest
        {
            Name = name,
            Description = description,
            SecretString = JsonHelper.Serialize(model)
        });

        if (res.HttpStatusCode != System.Net.HttpStatusCode.OK)
            throw new Exception("Ocorreu um erro ao criar o secret.");

        return res.ARN;
    }

    private class SecretModel
    {
        [JsonProperty("engine")]
        public string Engine { get; set; }
        [JsonProperty("host")]
        public string Host { get; set; }
        [JsonProperty("port")]
        public string Port { get; set; }
        [JsonProperty("dbname")]
        public string DbName { get; set; }
        [JsonProperty("username")]
        public string Username { get; set; }
        [JsonProperty("password")]
        public string Password { get; set; }
    }
}
