using Techer.Aws.Shared;
using Techer.Common.Domain.Enums;
using Techer.Common.Domain.Interfaces;

namespace Arauco.Otimizador.Aws.CloudFront;

public static class CloudFrontHelper
{
    public static string GetCustomSignedUrl(IEnvironmentVariables env, AwsStringResource bucket, string key, string outputFileName, int expiryMinutes = 30, bool viewInBrowser = false)
    {
        string? url = null;

        string contentDisp = viewInBrowser ? "inline" : "attachment";
        string file = string.Format("{0}?response-content-disposition={1};filename={2}",
            key,
            contentDisp,
            Uri.EscapeDataString(outputFileName));

        string accessKey;
        switch (env.GetEnvironmentEnum())
        {
            case EnvironmentEnum.Dev:
                accessKey = "##########";
                break;

            case EnvironmentEnum.Test:
                accessKey = "##########";
                break;

            default:
                accessKey = "##########";
                break;
        }

        string privateKeyName = $"pk-{accessKey}.pem";
        string customDomain = bucket.Get(env.GetEnvironmentEnum()) switch
        {
            "arauco-otimizador-dev" => "storage.dev.hub.arauco.app.br",
            "arauco-otimizador-temp-dev" => "storage-temp.dev.hub.arauco.app.br",
            "arauco-otimizador-prod" => "storage.hub.arauco.app.br",
            "arauco-otimizador-temp-prod" => "storage-temp.hub.arauco.app.br",
            _ => throw new Exception("Bucket sem domínio personalizado."),
        };

        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        using (var streamReader = new StreamReader(assembly.GetManifestResourceStream($"Arauco.Otimizador.Aws.CloudFront.Certificates.{privateKeyName}")))
        {
            url = Amazon.CloudFront.AmazonCloudFrontUrlSigner.GetCustomSignedURL(
                Amazon.CloudFront.AmazonCloudFrontUrlSigner.Protocol.https,
                customDomain,
                streamReader,
                file,
                accessKey,
                DateTime.UtcNow.AddMinutes(expiryMinutes),
                null);
        }

        return url;
    }
}
