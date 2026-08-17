namespace Arauco.Otimizador.Common.Domain.Models.Otimizador;

// Enviado em POST /cenarios/{id}/otimizar. Horizonte, capacidade, semana inicial, alvo de capacidade
// sobre demanda, carreta mín/máx e limite de recebimento vêm do Setup vinculado ao cenário — não são
// mais overrides por chamada (ver OtimizadorService.CriarConfig). `limiteSegundos` continua aqui por
// ser um knob operacional do solver, não uma regra de negócio do Setup.
public class OtimizacaoRequest
{
    public double? LimiteSegundos { get; set; }
}
