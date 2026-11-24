using Caramello.Api.Domain;

namespace Caramello.Api.Dtos;

public record InsumoDto(
    int Id,
    string Nome,
    string Categoria,
    string Unidade,
    decimal Quantidade,
    decimal EstoqueMinimo,
    int PrazoReposicaoDias,
    decimal EstoqueSeguranca)
{
    public static InsumoDto Criar(Insumo item) => new(
        item.Id,
        item.Nome,
        item.Categoria,
        item.Unidade,
        item.Quantidade,
        item.EstoqueMinimo,
        item.PrazoReposicaoDias,
        item.EstoqueSeguranca);
}
