namespace Caramello.Api.Domain;

public class Descarte
{
    public int Id { get; set; }
    public string Material { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public string Unidade { get; set; } = "un";
    public string Motivo { get; set; } = string.Empty;
    public DateTime Data { get; set; }
}
