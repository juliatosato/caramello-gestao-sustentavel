using Caramello.Api.Domain;

namespace Caramello.Api.Dtos;

public record MovimentacaoInput(TipoMovimentacao Tipo, decimal Quantidade, string? Motivo);
