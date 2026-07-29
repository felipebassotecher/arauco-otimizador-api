using Techer.Common.Domain.Interfaces;

namespace Arauco.Otimizador.Common.Storage;

// Salva arquivos recebidos pela API no disco local do servidor.
//
// Diretório base configurável via appsettings ("FileStorage": { "BasePath": "..." }); quando ausente,
// cai no diretório temporário do SO. Isso é o que existe de gravável hoje em AWS Lambda (/tmp) — mas
// esse disco é efêmero (não sobrevive entre invocações/cold starts) e não é compartilhado entre
// instâncias concorrentes da função. Para persistência real em produção, o destino correto é o S3
// (o projeto já provisiona buckets — veja `Techer.Aws.Storage.S3Helper`); trocar a implementação deste
// Helper por uma baseada em S3 quando isso for necessário, sem precisar alterar quem o chama.
public static class LocalFileStorageHelper
{
    private const string BasePathConfigKey = "FileStorage:BasePath";
    private const string DefaultFolderName = "arauco-otimizador";

    public static async Task<string> SaveAsync(IEnvironmentVariables env, string subpasta, string nomeArquivo, Stream conteudo)
    {
        var directory = Path.Combine(GetBasePath(env), subpasta);

        Directory.CreateDirectory(directory);

        var fullPath = Path.Combine(directory, nomeArquivo);

        await using var fileStream = File.Create(fullPath);
        await conteudo.CopyToAsync(fileStream);

        return fullPath;
    }

    private static string GetBasePath(IEnvironmentVariables env)
    {
        var basePath = env[BasePathConfigKey];

        return string.IsNullOrWhiteSpace(basePath)
            ? Path.Combine(Path.GetTempPath(), DefaultFolderName)
            : basePath;
    }
}
