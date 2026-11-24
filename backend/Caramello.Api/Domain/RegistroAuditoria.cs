namespace Caramello.Api.Domain;

public sealed class RegistroAuditoria
{
    public long Id { get; set; }
    public string Usuario { get; set; } = "sistema";
    public DateTime DataUtc { get; set; }
    public string Operacao { get; set; } = string.Empty;
    public string Entidade { get; set; } = string.Empty;
    public string IdentificadorRegistro { get; set; } = string.Empty;
}
