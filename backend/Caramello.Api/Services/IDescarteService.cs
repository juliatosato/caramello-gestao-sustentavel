using Caramello.Api.Domain;
using Caramello.Api.Dtos;

namespace Caramello.Api.Services;

public interface IDescarteService
{
    Task<IReadOnlyList<Descarte>> ListarAsync(CancellationToken cancellationToken);
    Task<Descarte> CriarAsync(DescarteInput input, CancellationToken cancellationToken);
}
