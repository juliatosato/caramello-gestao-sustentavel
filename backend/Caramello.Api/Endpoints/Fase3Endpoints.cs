using Caramello.Api.Dtos;
using Caramello.Api.Services;

namespace Caramello.Api.Endpoints;

public static class Fase3Endpoints
{
    public static IEndpointRouteBuilder MapFase3Endpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/indicadores", async (
            DateTime? inicio,
            DateTime? fim,
            IIndicadoresService service,
            CancellationToken ct) =>
        {
            var periodo = Periodo(inicio, fim);
            if (periodo.Erro is not null) return Results.BadRequest(new { mensagem = periodo.Erro });
            return Results.Ok(await service.ObterDashboardAsync(periodo.Inicio, periodo.Fim, ct));
        });

        app.MapGet("/api/desperdicios", async (
            DateTime? inicio,
            DateTime? fim,
            string? categoria,
            string? motivo,
            IIndicadoresService service,
            CancellationToken ct) =>
        {
            var periodo = Periodo(inicio, fim);
            if (periodo.Erro is not null) return Results.BadRequest(new { mensagem = periodo.Erro });
            return Results.Ok(await service.AnalisarDesperdicioAsync(
                periodo.Inicio, periodo.Fim, categoria?.Trim(), motivo?.Trim(), ct));
        });

        app.MapGet("/api/sugestoes-reposicao", async (
            DateTime? fim,
            int? dias,
            IIndicadoresService service,
            CancellationToken ct) =>
        {
            var quantidadeDias = dias ?? 30;
            if (quantidadeDias is < 7 or > 365)
                return Results.BadRequest(new { mensagem = "O período deve estar entre 7 e 365 dias." });
            var dataFim = (fim ?? DateTime.Today).Date;
            return Results.Ok(await service.ObterSugestoesAsync(dataFim, quantidadeDias, ct));
        });

        app.MapGet("/api/relatorios/comparacao", async (
            DateTime? inicio,
            DateTime? fim,
            IIndicadoresService service,
            CancellationToken ct) =>
        {
            var periodo = Periodo(inicio, fim);
            if (periodo.Erro is not null) return Results.BadRequest(new { mensagem = periodo.Erro });
            return Results.Ok(await service.CompararAsync(periodo.Inicio, periodo.Fim, ct));
        });

        app.MapGet("/api/relatorios/movimentacoes.csv", async (
            DateTime? inicio,
            DateTime? fim,
            IIndicadoresService service,
            CancellationToken ct) =>
        {
            var periodo = Periodo(inicio, fim);
            if (periodo.Erro is not null) return Results.BadRequest(new { mensagem = periodo.Erro });
            var arquivo = await service.ExportarMovimentacoesCsvAsync(periodo.Inicio, periodo.Fim, ct);
            return Results.File(arquivo, "text/csv; charset=utf-8", $"movimentacoes-{periodo.Inicio:yyyyMMdd}-{periodo.Fim:yyyyMMdd}.csv");
        });

        app.MapGet("/api/relatorios/indicadores.csv", async (
            DateTime? inicio,
            DateTime? fim,
            IIndicadoresService service,
            CancellationToken ct) =>
        {
            var periodo = Periodo(inicio, fim);
            if (periodo.Erro is not null) return Results.BadRequest(new { mensagem = periodo.Erro });
            var arquivo = await service.ExportarIndicadoresCsvAsync(periodo.Inicio, periodo.Fim, ct);
            return Results.File(arquivo, "text/csv; charset=utf-8", $"indicadores-{periodo.Inicio:yyyyMMdd}-{periodo.Fim:yyyyMMdd}.csv");
        });

        app.MapGet("/api/borra", async (
            DateTime? inicio,
            DateTime? fim,
            IBorraService service,
            CancellationToken ct) =>
        {
            var periodo = Periodo(inicio, fim);
            if (periodo.Erro is not null) return Results.BadRequest(new { mensagem = periodo.Erro });
            return Results.Ok(await service.ListarAsync(periodo.Inicio, periodo.Fim, ct));
        });

        app.MapPost("/api/borra", async (
            RegistroBorraInput input,
            IBorraService service,
            CancellationToken ct) =>
        {
            var erro = Validacao.Validar(input);
            if (erro is not null) return Results.BadRequest(new { mensagem = erro });
            var item = await service.CriarAsync(input, ct);
            return Results.Created($"/api/borra/{item.Id}", item);
        });

        return app;
    }

    private static (DateTime Inicio, DateTime Fim, string? Erro) Periodo(DateTime? inicio, DateTime? fim)
    {
        var dataFim = (fim ?? DateTime.Today).Date;
        var dataInicio = (inicio ?? dataFim.AddDays(-29)).Date;
        if (dataInicio > dataFim) return (dataInicio, dataFim, "A data inicial deve ser anterior à data final.");
        if ((dataFim - dataInicio).TotalDays > 366) return (dataInicio, dataFim, "O intervalo máximo é de 367 dias.");
        return (dataInicio, dataFim, null);
    }
}
