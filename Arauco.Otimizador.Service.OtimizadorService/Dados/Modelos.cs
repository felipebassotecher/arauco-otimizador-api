namespace Arauco.Otimizador.Service.OtimizadorService.Dados;

public sealed record LinhaCarteira(
    long CarteiraId,
    string ClienteId,
    string ClienteNome,
    string ProdutoId,
    int LinhaProdutoId,
    double VolumeM3,
    DateTime DataDocumento,
    string Incoterms,
    string Segmento,
    long CentroOriginal);

// Piso: volume mínimo de um fragmento deste item dentro de um embarque — max(lote mínimo cadastrado
// do produto, mínimo por SKU do setup), nunca maior que o próprio volume do item (ver
// Modelo/Preparacao.cs). Item.Piso == Item.VolumeM3 significa item indivisível (tudo-ou-nada, um
// único bucket); Item.Piso < Item.VolumeM3 significa item divisível entre vários buckets, nunca
// fragmentado abaixo do piso (ver Modelo/Otimizacao.cs).
public sealed record Item(
    int Indice,
    string ClienteId,
    string ClienteNome,
    string ProdutoId,
    int LinhaProdutoId,
    double VolumeM3,
    DateTime DataDocumentoMaisAntiga,
    bool Cif,
    bool Industria,
    double Piso,
    IReadOnlyList<int> CentrosElegiveis,
    IReadOnlyList<long> CarteiraIds);

public sealed record Produto(
    string ProdutoId,
    string Descricao,
    int LinhaProdutoId,
    double LoteMinimoChapas,
    double LarguraMm,
    double ComprimentoMm,
    double EspessuraMm,
    bool Ativo)
{
    public double ChapaM3 =>
        LarguraMm / 1000.0 * (ComprimentoMm / 1000.0) * (EspessuraMm / 1000.0);
}

public sealed record Centro(
    int CentroId,
    string Codigo,
    string Nome,
    bool Ativo,
    int PorcentagemIndustria,
    int PorcentagemRevenda);

public sealed record CapacidadeSemanal(
    int CentroId,
    int LinhaProdutoId,
    SemanaIso Semana,
    long Quantidade);

public readonly record struct SemanaIso(int Ano, int Numero) : IComparable<SemanaIso>
{
    public int CompareTo(SemanaIso outra) =>
        Ano != outra.Ano ? Ano.CompareTo(outra.Ano) : Numero.CompareTo(outra.Numero);

    public override string ToString() => $"{Ano}-W{Numero:D2}";

    public static SemanaIso De(DateTime data) => new(
        System.Globalization.ISOWeek.GetYear(data),
        System.Globalization.ISOWeek.GetWeekOfYear(data));

    public static SemanaIso Parse(string texto)
    {
        var partes = texto.Split('-', 'W');
        return new SemanaIso(int.Parse(partes[0]), int.Parse(partes[2]));
    }

    public SemanaIso Somar(int n)
    {
        var ano = Ano;
        var numero = Numero + n;

        while (numero > System.Globalization.ISOWeek.GetWeeksInYear(ano))
        {
            numero -= System.Globalization.ISOWeek.GetWeeksInYear(ano);
            ano++;
        }
        while (numero < 1)
        {
            ano--;
            numero += System.Globalization.ISOWeek.GetWeeksInYear(ano);
        }

        return new SemanaIso(ano, numero);
    }
}
