namespace Caramello.Api.Dtos;

public record InsumoEdicaoInput(
    string Nome,
    string Categoria,
    string Unidade,
    decimal EstoqueMinimo,
    int PrazoReposicaoDias,
    decimal EstoqueSeguranca);
