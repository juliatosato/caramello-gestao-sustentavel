using Caramello.Api.Domain;

namespace Caramello.Api.Dtos;

public record RegistroBorraInput(
    TipoRegistroBorra Tipo,
    decimal Quantidade,
    string Unidade,
    string? Destinacao,
    string? Observacao,
    DateTime Data);
