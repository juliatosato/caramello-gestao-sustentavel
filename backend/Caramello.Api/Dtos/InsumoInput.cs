namespace Caramello.Api.Dtos;

public record InsumoInput(
    string Nome,
    string Categoria,
    string Unidade,
    decimal Quantidade,
    decimal EstoqueMinimo,
    int PrazoReposicaoDias = 0,
    decimal EstoqueSeguranca = 0);
