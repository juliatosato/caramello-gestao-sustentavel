using Caramello.Api.Dtos;
using Caramello.Api.Infrastructure;
using Caramello.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Caramello.Api.Endpoints;

public static class CaramelloEndpoints
{
    public static IEndpointRouteBuilder MapCaramelloEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", async (AppDbContext db, CancellationToken ct) =>
        {
            try
            {
                var conectado = await db.Database.CanConnectAsync(ct);
                var migrationsPendentes = conectado && (await db.Database.GetPendingMigrationsAsync(ct)).Any();
                return conectado && !migrationsPendentes
                    ? Results.Ok(new { status = "ok" })
                    : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            catch
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }).AllowAnonymous();

        var insumos = app.MapGroup("/api/insumos");
        insumos.MapGet("/", async (IInsumoService service, CancellationToken ct) =>
            Results.Ok((await service.ListarAsync(ct)).Select(InsumoDto.Criar)));
        insumos.MapGet("/{id:int}", async (int id, IInsumoService service, CancellationToken ct) =>
        {
            var item = await service.ObterAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(InsumoDto.Criar(item));
        });
        insumos.MapPost("/", async (InsumoInput input, IInsumoService service, CancellationToken ct) =>
        {
            var erro = Validacao.Validar(input);
            if (erro is not null) return Results.BadRequest(new { mensagem = erro });
            var item = await service.CriarAsync(input, ct);
            return Results.Created($"/api/insumos/{item.Id}", InsumoDto.Criar(item));
        });
        insumos.MapPut("/{id:int}", async (int id, InsumoEdicaoInput input, IInsumoService service, CancellationToken ct) =>
        {
            var erro = Validacao.Validar(input);
            if (erro is not null) return Results.BadRequest(new { mensagem = erro });
            try
            {
                var item = await service.EditarAsync(id, input, ct);
                return item is null ? Results.NotFound() : Results.Ok(InsumoDto.Criar(item));
            }
            catch (RegraNegocioException exception)
            {
                return Results.Conflict(new { mensagem = exception.Message });
            }
        });
        insumos.MapDelete("/{id:int}", async (int id, IInsumoService service, CancellationToken ct) =>
            await service.DesativarAsync(id, ct) ? Results.NoContent() : Results.NotFound());
        insumos.MapPost("/{id:int}/movimentacoes", async (
            int id,
            MovimentacaoInput input,
            IInsumoService service,
            CancellationToken ct) =>
        {
            var erro = Validacao.Validar(input);
            if (erro is not null) return Results.BadRequest(new { mensagem = erro });
            try
            {
                var movimentacao = await service.MovimentarAsync(id, input, ct);
                if (movimentacao is null) return Results.NotFound();
                var insumo = await service.ObterAsync(id, ct);
                var resposta = new MovimentacaoDto(
                    movimentacao.Id,
                    movimentacao.InsumoId,
                    insumo!.Nome,
                    movimentacao.Unidade,
                    movimentacao.Tipo,
                    movimentacao.Quantidade,
                    movimentacao.Motivo,
                    movimentacao.Data);
                return Results.Created($"/api/movimentacoes/{movimentacao.Id}", resposta);
            }
            catch (RegraNegocioException exception)
            {
                return Results.Conflict(new { mensagem = exception.Message });
            }
        });

        app.MapGet("/api/movimentacoes", async (
            int? insumoId,
            IInsumoService service,
            CancellationToken ct) => Results.Ok(await service.ListarMovimentacoesAsync(insumoId, ct)));

        var descartes = app.MapGroup("/api/descartes");
        descartes.MapGet("/", async (IDescarteService service, CancellationToken ct) =>
            Results.Ok(await service.ListarAsync(ct)));
        descartes.MapPost("/", async (DescarteInput input, IDescarteService service, CancellationToken ct) =>
        {
            var erro = Validacao.Validar(input);
            if (erro is not null) return Results.BadRequest(new { mensagem = erro });
            var item = await service.CriarAsync(input, ct);
            return Results.Created($"/api/descartes/{item.Id}", item);
        });

        app.MapGet("/api/resumo", async (AppDbContext db, CancellationToken ct) =>
        {
            var resumo = new ResumoDto(
                await db.Insumos.CountAsync(ct),
                await db.Insumos.CountAsync(x => x.Quantidade <= x.EstoqueMinimo, ct),
                await db.Descartes.CountAsync(ct),
                await db.Descartes
                    .Where(x => x.Material.ToLower().Contains("borra") && x.Unidade == "kg")
                    .SumAsync(x => (decimal?)x.Quantidade, ct) ?? 0m);
            return Results.Ok(resumo);
        });

        return app;
    }
}
