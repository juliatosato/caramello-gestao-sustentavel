namespace Caramello.Api.Dtos;

public record DescarteInput(string Material, string Unidade, decimal Quantidade, string? Motivo);
