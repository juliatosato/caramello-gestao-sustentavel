using Caramello.Api.Domain;

namespace Caramello.Api.Dtos;

public record MovimentacaoDto(
    int Id,
    int InsumoId,
    string Insumo,
    string Unidade,
    TipoMovimentacao Tipo,
    decimal Quantidade,
    string Motivo,
    DateTime Data);
