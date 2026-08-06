using Arauco.Otimizador.Service.OtimizadorService.Capacidade;
using Arauco.Otimizador.Service.OtimizadorService.Dados;

namespace Arauco.Otimizador.Service.OtimizadorService.Modelo;

public sealed record ExplicacaoAlocacao(
    string MotivoSemana,
    string MotivoPlanta,
    double FolgaAntesM3,
    int PlantasElegiveis,
    int PosicaoPrioridade);

public sealed record ExplicacaoSaldo(string Motivo, double MaiorFolgaM3, double PisoM3);

public static class Explicador
{
    public static (Dictionary<(int Item, int Centro, int Semana), ExplicacaoAlocacao> Alocacoes,
                   Dictionary<int, ExplicacaoSaldo> Saldos)
        Explicar(
            IReadOnlyList<Item> itens,
            CapacidadeHorizonte capacidade,
            IReadOnlyList<Alocacao> alocacoes,
            IReadOnlyDictionary<int, double> naoAlocado,
            IReadOnlyDictionary<int, long> prioridades)
    {
        var semanas = capacidade.Semanas.Count;
        var porIndice = itens.ToDictionary(i => i.Indice);

        var folga = new Dictionary<(int Centro, int LinhaProduto, int Semana), double>();
        foreach (var (chave, valor) in capacidade.PorBucket)
            folga[(chave.Centro, chave.LinhaProduto, chave.IndiceSemana)] = valor;

        foreach (var a in alocacoes)
        {
            var chave = (a.CentroId, porIndice[a.ItemIndice].LinhaProdutoId, a.IndiceSemana);
            if (folga.ContainsKey(chave)) folga[chave] -= a.VolumeM3;
        }

        var posicao = itens
            .OrderByDescending(i => prioridades[i.Indice])
            .Select((i, n) => (i.Indice, Posicao: n + 1))
            .ToDictionary(t => t.Indice, t => t.Posicao);

        var explicacoes = new Dictionary<(int, int, int), ExplicacaoAlocacao>();

        foreach (var a in alocacoes)
        {
            var item = porIndice[a.ItemIndice];
            var lp = item.LinhaProdutoId;
            var piso = item.LoteMinimoM3;

            var elegiveisComCapacidade = item.CentrosElegiveis
                .Count(c => Enumerable.Range(0, semanas)
                    .Any(s => capacidade.Disponivel(c, lp, s) > 0));

            var motivoPlanta = elegiveisComCapacidade <= 1
                ? Motivos.PlantaUnica
                : string.Format(Motivos.PlantaEscolhida, elegiveisComCapacidade);

            double maiorFolgaAntes = 0;
            for (var s = 0; s < a.IndiceSemana; s++)
                foreach (var c in item.CentrosElegiveis)
                    maiorFolgaAntes = Math.Max(maiorFolgaAntes, folga.GetValueOrDefault((c, lp, s), 0));

            var motivoSemana = a.IndiceSemana == 0
                ? Motivos.PrimeiraSemana
                : maiorFolgaAntes < 0.01
                    ? Motivos.SemCapacidadeAntes
                    : maiorFolgaAntes < piso
                        ? Motivos.LoteMinimoNaoCabe
                        : Motivos.DeslocamentoPossivel;

            explicacoes[(a.ItemIndice, a.CentroId, a.IndiceSemana)] = new ExplicacaoAlocacao(
                motivoSemana, motivoPlanta, Math.Round(maiorFolgaAntes, 2),
                elegiveisComCapacidade, posicao[a.ItemIndice]);
        }

        var saldos = new Dictionary<int, ExplicacaoSaldo>();

        foreach (var (indice, volume) in naoAlocado)
        {
            var item = porIndice[indice];
            var lp = item.LinhaProdutoId;
            var piso = item.LoteMinimoM3;

            double maiorFolga = 0;
            for (var s = 0; s < semanas; s++)
                foreach (var c in item.CentrosElegiveis)
                    maiorFolga = Math.Max(maiorFolga, folga.GetValueOrDefault((c, lp, s), 0));

            var motivo = maiorFolga < 0.01
                ? Motivos.NaoAlocadoSemCapacidade
                : maiorFolga < piso
                    ? Motivos.NaoAlocadoLoteMinimo
                    : Motivos.NaoAlocadoPrioridade;

            saldos[indice] = new ExplicacaoSaldo(motivo, Math.Round(maiorFolga, 2), Math.Round(piso, 2));
            _ = volume;
        }

        return (explicacoes, saldos);
    }
}
