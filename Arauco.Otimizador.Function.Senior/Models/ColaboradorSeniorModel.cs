namespace Arauco.Function.Senior.Models;

public class ColaboradorSeniorModel
{
    public string? CentroCusto { get; set; }
    public int? FilialCodigo { get; set; }
    public string FilialNome { get; set; }
    public string CodigoPostoTrabalho { get; set; }
    public string PostoTrabalhoNome { get; set; }
    public DateOnly? DataNascimento { get; set; }
    public bool Ferias { get; set; }
    public string? MatriculaGestor { get; set; }
    public string Nome { get; set; }
    public string Matricula { get; set; }
    public int? NumeroEmpresa { get; set; }
    public string? Celular { get; set; }
    public string Status { get; set; }
    public string NumeroCracha { get; set; }
    public string? Genero { get; set; }
    public string? Cpf { get; set; }
    public int? TipoColaborador { get; set; }
    public string? EmailComercial { get; set; }
    public string? EmailParticular { get; set; }
    public string? Telefone2 { get; set; }
    public string? CodigoCargo { get; set; }
    public string? DescricaoCargo { get; set; }

    public int? EmpresaId { get; set; }
    public int? CentroCustoId { get; set; }
    public int? CargoId { get; set; }
    public int? PostoTrabalhoId { get; set; }
    public int? SituacaoColaboradorId { get; set; }
}
