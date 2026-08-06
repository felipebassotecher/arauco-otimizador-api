namespace Arauco.Otimizador.Common.Domain.Enums.Criterio;

// Chave fechada dos critérios disponíveis (changelog 2026-08-03). `criterioChave` deixou de ser string
// livre e passou a ser este enum (int) — valores fora dele são rejeitados com 400. Hoje há um único
// critério. C# não suporta enum de string, então a chave é um inteiro: TipoFrete = 1. O nome legível
// ("Tipo de Frete") e o tipo são expostos por GET /cenarios/criterios-disponiveis (CriteriosDisponiveis).
public enum CriterioChaveEnum
{
    TipoFrete = 1,
    TipoCliente = 2,
}