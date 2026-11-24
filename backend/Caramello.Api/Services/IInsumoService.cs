using Caramello.Api.Domain;
using Caramello.Api.Dtos;

namespace Caramello.Api.Services;

public interface IInsumoService
{
    Task<IReadOnlyList<Insumo>> ListarAsync(CancellationToken cancellationToken);
    Task<Insumo?> ObterAsync(int id, CancellationToken cancellationToken);
    Task<Insumo> CriarAsync(InsumoInput input, CancellationToken cancellationToken);
    Task<Insumo?> EditarAsync(int id, InsumoEdicaoInput input, CancellationToken cancellationToken);
    Task<bool> DesativarAsync(int id, CancellationToken cancellationToken);
    Task<MovimentacaoEstoque?> MovimentarAsync(int id, MovimentacaoInput input, CancellationToken cancellationToken);
    Task<IReadOnlyList<MovimentacaoDto>> ListarMovimentacoesAsync(int? insumoId, CancellationToken cancellationToken);
}
