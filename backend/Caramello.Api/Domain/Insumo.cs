namespace Caramello.Api.Domain;

public class Insumo
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Unidade { get; set; } = "un";
    public decimal Quantidade { get; set; }
    public decimal EstoqueMinimo { get; set; }
    public int PrazoReposicaoDias { get; set; }
    public decimal EstoqueSeguranca { get; set; }
    public bool Ativo { get; set; } = true;
    public ICollection<MovimentacaoEstoque> Movimentacoes { get; set; } = [];
}
