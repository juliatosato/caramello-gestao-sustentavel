namespace Caramello.Api.Domain;

public class RegistroBorra
{
    public int Id { get; set; }
    public TipoRegistroBorra Tipo { get; set; }
    public decimal Quantidade { get; set; }
    public string Unidade { get; set; } = "kg";
    public string Destinacao { get; set; } = string.Empty;
    public string Observacao { get; set; } = string.Empty;
    public DateTime Data { get; set; }
}
