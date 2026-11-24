using Caramello.Api.Domain;
using Caramello.Api.Dtos;
using Caramello.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Caramello.Api.Services;

public class DescarteService(AppDbContext db) : IDescarteService
{
    public async Task<IReadOnlyList<Descarte>> ListarAsync(CancellationToken cancellationToken) =>
        await db.Descartes.AsNoTracking().OrderByDescending(x => x.Data).ToListAsync(cancellationToken);

    public async Task<Descarte> CriarAsync(DescarteInput input, CancellationToken cancellationToken)
    {
        var item = new Descarte
        {
            Material = input.Material.Trim(),
            Quantidade = input.Quantidade,
            Unidade = input.Unidade,
            Motivo = input.Motivo?.Trim() ?? string.Empty,
            Data = DateTime.UtcNow
        };
        db.Descartes.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return item;
    }
}
