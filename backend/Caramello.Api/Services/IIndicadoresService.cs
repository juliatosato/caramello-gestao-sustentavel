using Caramello.Api.Dtos;

namespace Caramello.Api.Services;

public interface IIndicadoresService
{
    Task<DashboardDto> ObterDashboardAsync(DateTime inicio, DateTime fim, CancellationToken ct);
    Task<AnaliseDesperdicioDto> AnalisarDesperdicioAsync(DateTime inicio, DateTime fim, string? categoria, string? motivo, CancellationToken ct);
    Task<IReadOnlyList<SugestaoReposicaoDto>> ObterSugestoesAsync(DateTime fim, int dias, CancellationToken ct);
    Task<ComparacaoPeriodoDto> CompararAsync(DateTime inicio, DateTime fim, CancellationToken ct);
    Task<byte[]> ExportarMovimentacoesCsvAsync(DateTime inicio, DateTime fim, CancellationToken ct);
    Task<byte[]> ExportarIndicadoresCsvAsync(DateTime inicio, DateTime fim, CancellationToken ct);
}
