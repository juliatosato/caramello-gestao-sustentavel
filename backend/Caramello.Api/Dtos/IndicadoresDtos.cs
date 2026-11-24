namespace Caramello.Api.Dtos;

public record TotalUnidadeDto(string Unidade, decimal Quantidade);
public record AgrupamentoDto(string Rotulo, string Unidade, decimal Quantidade);
public record SerieTemporalDto(DateOnly Data, string Unidade, decimal Quantidade);
public record EstoqueBaixoDto(int Id, string Nome, string Categoria, string Unidade, decimal Saldo, decimal Minimo);
public record DashboardDto(
    DateTime Inicio,
    DateTime Fim,
    IReadOnlyList<TotalUnidadeDto> Descartes,
    IReadOnlyList<TotalUnidadeDto> Consumo,
    IReadOnlyList<AgrupamentoDto> DescartesPorCategoria,
    IReadOnlyList<AgrupamentoDto> DescartesPorMotivo,
    IReadOnlyList<EstoqueBaixoDto> EstoqueBaixo,
    IReadOnlyList<SerieTemporalDto> EvolucaoDescartes);
public record DesperdicioInsumoDto(
    int InsumoId,
    string Insumo,
    string Categoria,
    string Unidade,
    decimal QuantidadeDescartada,
    decimal SaidasTotais,
    decimal? ProporcaoDescarte);
public record AnaliseDesperdicioDto(
    DateTime Inicio,
    DateTime Fim,
    IReadOnlyList<DesperdicioInsumoDto> Insumos,
    IReadOnlyList<SerieTemporalDto> Evolucao);
public record SugestaoReposicaoDto(
    int InsumoId,
    string Insumo,
    string Unidade,
    decimal SaldoAtual,
    int PrazoReposicaoDias,
    decimal EstoqueSeguranca,
    int DiasComHistorico,
    decimal? ConsumoMedioDiario,
    decimal? QuantidadeSugerida,
    bool HistoricoSuficiente,
    string Mensagem,
    string Metodologia);
public record ComparacaoPeriodoDto(
    DashboardDto PeriodoAtual,
    DashboardDto PeriodoAnterior);
