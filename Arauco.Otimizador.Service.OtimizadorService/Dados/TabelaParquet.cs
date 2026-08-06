using Parquet;
using Parquet.Serialization;

namespace Arauco.Otimizador.Service.OtimizadorService.Dados;

public sealed class TabelaParquet
{
    private readonly IList<Dictionary<string, object>> _linhas;
    private readonly HashSet<string> _colunas;

    public int Linhas => _linhas.Count;

    private TabelaParquet(IList<Dictionary<string, object>> linhas, IEnumerable<string> colunas)
    {
        _linhas = linhas;
        _colunas = new HashSet<string>(colunas, StringComparer.OrdinalIgnoreCase);
    }

    public static async Task<TabelaParquet> LerAsync(string caminho)
    {
        if (!File.Exists(caminho))
            throw new FileNotFoundException($"Parquet não encontrado: {Path.GetFullPath(caminho)}");

        await using var paraSchema = File.OpenRead(caminho);
        await using var reader = await ParquetReader.CreateAsync(paraSchema);
        var colunas = reader.Schema.DataFields.Select(f => f.Name).ToList();

        await using var stream = File.OpenRead(caminho);
        var resultado = await ParquetSerializer.DeserializeUntypedAsync(stream);
        return new TabelaParquet(resultado.Data.ToList(), colunas);
    }

    private object? Valor(int linha, string coluna)
    {
        if (!_colunas.Contains(coluna))
            throw new KeyNotFoundException(
                $"Coluna '{coluna}' não existe. Disponíveis: {string.Join(", ", _colunas)}");

        var registro = _linhas[linha];
        if (registro.TryGetValue(coluna, out var v)) return v;

        foreach (var (chave, valor) in registro)
            if (string.Equals(chave, coluna, StringComparison.OrdinalIgnoreCase))
                return valor;

        return null;
    }

    private T?[] Mapear<T>(string coluna, Func<object, T> conversor) where T : struct
    {
        var saida = new T?[Linhas];
        for (var i = 0; i < Linhas; i++)
        {
            var v = Valor(i, coluna);
            saida[i] = v is null ? null : conversor(v);
        }
        return saida;
    }

    public string?[] Texto(string coluna)
    {
        var saida = new string?[Linhas];
        for (var i = 0; i < Linhas; i++)
            saida[i] = Valor(i, coluna)?.ToString();
        return saida;
    }

    public long?[] Inteiro(string coluna) => Mapear(coluna, Convert.ToInt64);

    public double?[] Decimal(string coluna) => Mapear(coluna, Convert.ToDouble);

    public bool?[] Booleano(string coluna) => Mapear(coluna, Convert.ToBoolean);

    public DateTime?[] Data(string coluna) => Mapear(coluna, v => v switch
    {
        DateTime d => d,
        DateTimeOffset o => o.DateTime,
        _ => Convert.ToDateTime(v),
    });
}
