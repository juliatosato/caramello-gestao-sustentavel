namespace Caramello.Api.Domain;

public class MovimentacaoEstoque
{
    public int Id { get; set; }
    public int InsumoId { get; set; }
    public Insumo Insumo { get; set; } = null!;
    public TipoMovimentacao Tipo { get; set; }
    public decimal Quantidade { get; set; }
    public string Unidade { get; set; } = "un";
    public string Motivo { get; set; } = string.Empty;
    public DateTime Data { get; set; }
}
