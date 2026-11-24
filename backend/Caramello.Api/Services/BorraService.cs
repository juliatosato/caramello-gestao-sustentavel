using Caramello.Api.Domain;
using Caramello.Api.Dtos;
using Caramello.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Caramello.Api.Services;

public class BorraService(AppDbContext db) : IBorraService
{
    public async Task<IReadOnlyList<RegistroBorra>> ListarAsync(DateTime inicio, DateTime fim, CancellationToken ct) =>
        await db.RegistrosBorra.AsNoTracking()
            .Where(x => x.Data >= inicio.Date && x.Data < fim.Date.AddDays(1))
            .OrderByDescending(x => x.Data)
            .ToListAsync(ct);

    public async Task<RegistroBorra> CriarAsync(RegistroBorraInput input, CancellationToken ct)
    {
        var item = new RegistroBorra
        {
            Tipo = input.Tipo,
            Quantidade = input.Quantidade,
            Unidade = input.Unidade,
            Destinacao = input.Tipo == TipoRegistroBorra.Destinada ? input.Destinacao?.Trim() ?? string.Empty : string.Empty,
            Observacao = input.Observacao?.Trim() ?? string.Empty,
            Data = DateTime.SpecifyKind(input.Data, DateTimeKind.Local).ToUniversalTime()
        };
        db.RegistrosBorra.Add(item);
        await db.SaveChangesAsync(ct);
        return item;
    }
}
