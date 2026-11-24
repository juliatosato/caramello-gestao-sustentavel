using Caramello.Api.Domain;
using Caramello.Api.Dtos;

namespace Caramello.Api.Services;

public interface IBorraService
{
    Task<IReadOnlyList<RegistroBorra>> ListarAsync(DateTime inicio, DateTime fim, CancellationToken ct);
    Task<RegistroBorra> CriarAsync(RegistroBorraInput input, CancellationToken ct);
}
