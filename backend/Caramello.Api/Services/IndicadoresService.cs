using System.Globalization;
using System.Text;
using Caramello.Api.Domain;
using Caramello.Api.Dtos;
using Caramello.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Caramello.Api.Services;

public class IndicadoresService(AppDbContext db) : IIndicadoresService
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");

    public async Task<DashboardDto> ObterDashboardAsync(DateTime inicio, DateTime fim, CancellationToken ct)
    {
        var fimExclusivo = fim.Date.AddDays(1);
        var movimentacoes = await db.MovimentacoesEstoque.AsNoTracking()
            .Include(x => x.Insumo)
            .Where(x => x.Data >= inicio.Date && x.Data < fimExclusivo)
            .ToListAsync(ct);
        var descartes = movimentacoes.Where(x => x.Tipo == TipoMovimentacao.Descarte).ToList();
        var consumos = movimentacoes.Where(x => x.Tipo == TipoMovimentacao.Consumo).ToList();
        var insumos = await db.Insumos.AsNoTracking().Where(x => x.Ativo).OrderBy(x => x.Nome).ToListAsync(ct);

        return new DashboardDto(
            inicio.Date,
            fim.Date,
            TotaisPorUnidade(descartes),
            TotaisPorUnidade(consumos),
            descartes.GroupBy(x => new { x.Insumo.Categoria, x.Unidade })
                .Select(x => new AgrupamentoDto(x.Key.Categoria, x.Key.Unidade, x.Sum(y => y.Quantidade)))
                .OrderByDescending(x => x.Quantidade).ToList(),
            descartes.GroupBy(x => new { Motivo = TextoOuNaoInformado(x.Motivo), x.Unidade })
                .Select(x => new AgrupamentoDto(x.Key.Motivo, x.Key.Unidade, x.Sum(y => y.Quantidade)))
                .OrderByDescending(x => x.Quantidade).ToList(),
            insumos.Where(x => x.Quantidade <= x.EstoqueMinimo)
                .Select(x => new EstoqueBaixoDto(x.Id, x.Nome, x.Categoria, x.Unidade, x.Quantidade, x.EstoqueMinimo)).ToList(),
            descartes.GroupBy(x => new { Data = DateOnly.FromDateTime(x.Data), x.Unidade })
                .Select(x => new SerieTemporalDto(x.Key.Data, x.Key.Unidade, x.Sum(y => y.Quantidade)))
                .OrderBy(x => x.Data).ToList());
    }

    public async Task<AnaliseDesperdicioDto> AnalisarDesperdicioAsync(
        DateTime inicio,
        DateTime fim,
        string? categoria,
        string? motivo,
        CancellationToken ct)
    {
        var fimExclusivo = fim.Date.AddDays(1);
        var query = db.MovimentacoesEstoque.AsNoTracking().Include(x => x.Insumo)
            .Where(x => x.Data >= inicio.Date && x.Data < fimExclusivo &&
                (x.Tipo == TipoMovimentacao.Consumo || x.Tipo == TipoMovimentacao.Descarte));
        if (!string.IsNullOrWhiteSpace(categoria)) query = query.Where(x => x.Insumo.Categoria == categoria);
        var saidas = await query.ToListAsync(ct);
        if (!string.IsNullOrWhiteSpace(motivo))
        {
            saidas = saidas.Where(x => x.Tipo != TipoMovimentacao.Descarte ||
                x.Motivo.Contains(motivo, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var itens = saidas.GroupBy(x => new { x.InsumoId, x.Insumo.Nome, x.Insumo.Categoria, x.Unidade })
            .Select(grupo =>
            {
                var descarte = grupo.Where(x => x.Tipo == TipoMovimentacao.Descarte).Sum(x => x.Quantidade);
                var total = grupo.Sum(x => x.Quantidade);
                return new DesperdicioInsumoDto(
                    grupo.Key.InsumoId,
                    grupo.Key.Nome,
                    grupo.Key.Categoria,
                    grupo.Key.Unidade,
                    descarte,
                    total,
                    total > 0 ? decimal.Round(descarte / total * 100m, 1) : null);
            })
            .Where(x => x.QuantidadeDescartada > 0)
            .OrderBy(x => x.Unidade).ThenByDescending(x => x.QuantidadeDescartada)
            .ToList();

        var evolucao = saidas.Where(x => x.Tipo == TipoMovimentacao.Descarte)
            .GroupBy(x => new { Data = DateOnly.FromDateTime(x.Data), x.Unidade })
            .Select(x => new SerieTemporalDto(x.Key.Data, x.Key.Unidade, x.Sum(y => y.Quantidade)))
            .OrderBy(x => x.Data).ToList();
        return new AnaliseDesperdicioDto(inicio.Date, fim.Date, itens, evolucao);
    }

    public async Task<IReadOnlyList<SugestaoReposicaoDto>> ObterSugestoesAsync(DateTime fim, int dias, CancellationToken ct)
    {
        var inicio = fim.Date.AddDays(-dias + 1);
        var fimExclusivo = fim.Date.AddDays(1);
        var insumos = await db.Insumos.AsNoTracking().Where(x => x.Ativo).OrderBy(x => x.Nome).ToListAsync(ct);
        var consumos = await db.MovimentacoesEstoque.AsNoTracking()
            .Where(x => x.Tipo == TipoMovimentacao.Consumo && x.Data >= inicio && x.Data < fimExclusivo)
            .ToListAsync(ct);

        return insumos.Select(insumo =>
        {
            var registros = consumos.Where(x => x.InsumoId == insumo.Id).ToList();
            var primeiro = registros.Count > 0 ? registros.Min(x => x.Data.Date) : fim.Date;
            var diasHistorico = registros.Count > 0 ? Math.Min(dias, (fim.Date - primeiro).Days + 1) : 0;
            var suficiente = registros.Count >= 2 && diasHistorico >= 7;
            decimal? media = suficiente ? registros.Sum(x => x.Quantidade) / diasHistorico : null;
            decimal? sugestao = media.HasValue
                ? Math.Max(0, decimal.Round(media.Value * insumo.PrazoReposicaoDias + insumo.EstoqueSeguranca - insumo.Quantidade, 3))
                : null;
            var mensagem = suficiente
                ? sugestao > 0 ? "Reposição sugerida pelos parâmetros configurados." : "O saldo atual cobre o período configurado."
                : "Histórico insuficiente: são necessários ao menos 7 dias e 2 registros de consumo.";
            return new SugestaoReposicaoDto(
                insumo.Id,
                insumo.Nome,
                insumo.Unidade,
                insumo.Quantidade,
                insumo.PrazoReposicaoDias,
                insumo.EstoqueSeguranca,
                diasHistorico,
                media.HasValue ? decimal.Round(media.Value, 3) : null,
                sugestao,
                suficiente,
                mensagem,
                "Consumo médio diário × prazo de reposição + estoque de segurança − saldo atual.");
        }).ToList();
    }

    public async Task<ComparacaoPeriodoDto> CompararAsync(DateTime inicio, DateTime fim, CancellationToken ct)
    {
        var duracao = (fim.Date - inicio.Date).Days + 1;
        var fimAnterior = inicio.Date.AddDays(-1);
        var inicioAnterior = fimAnterior.AddDays(-duracao + 1);
        return new ComparacaoPeriodoDto(
            await ObterDashboardAsync(inicio, fim, ct),
            await ObterDashboardAsync(inicioAnterior, fimAnterior, ct));
    }

    public async Task<byte[]> ExportarMovimentacoesCsvAsync(DateTime inicio, DateTime fim, CancellationToken ct)
    {
        var fimExclusivo = fim.Date.AddDays(1);
        var dados = await db.MovimentacoesEstoque.AsNoTracking().Include(x => x.Insumo)
            .Where(x => x.Data >= inicio.Date && x.Data < fimExclusivo)
            .OrderBy(x => x.Data).ToListAsync(ct);
        var csv = new StringBuilder("Data;Insumo;Categoria;Tipo;Quantidade;Unidade;Motivo\r\n");
        foreach (var item in dados)
        {
            csv.Append(Campo(item.Data.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Cultura))).Append(';')
                .Append(Campo(item.Insumo.Nome)).Append(';').Append(Campo(item.Insumo.Categoria)).Append(';')
                .Append(item.Tipo).Append(';').Append(item.Quantidade.ToString("0.###", Cultura)).Append(';')
                .Append(Campo(item.Unidade)).Append(';').Append(Campo(item.Motivo)).Append("\r\n");
        }
        return ComBom(csv.ToString());
    }

    public async Task<byte[]> ExportarIndicadoresCsvAsync(DateTime inicio, DateTime fim, CancellationToken ct)
    {
        var dashboard = await ObterDashboardAsync(inicio, fim, ct);
        var csv = new StringBuilder("Indicador;Período inicial;Período final;Categoria;Quantidade;Unidade\r\n");
        foreach (var item in dashboard.Consumo)
            LinhaIndicador(csv, "Consumo", dashboard, "", item.Quantidade, item.Unidade);
        foreach (var item in dashboard.Descartes)
            LinhaIndicador(csv, "Descarte", dashboard, "", item.Quantidade, item.Unidade);
        foreach (var item in dashboard.DescartesPorCategoria)
            LinhaIndicador(csv, "Descarte por categoria", dashboard, item.Rotulo, item.Quantidade, item.Unidade);
        return ComBom(csv.ToString());
    }

    private static IReadOnlyList<TotalUnidadeDto> TotaisPorUnidade(IEnumerable<MovimentacaoEstoque> itens) =>
        itens.GroupBy(x => x.Unidade).Select(x => new TotalUnidadeDto(x.Key, x.Sum(y => y.Quantidade)))
            .OrderBy(x => x.Unidade).ToList();

    private static string TextoOuNaoInformado(string texto) => string.IsNullOrWhiteSpace(texto) ? "Não informado" : texto;
    private static string Campo(string valor) => $"\"{valor.Replace("\"", "\"\"")}\"";
    private static byte[] ComBom(string valor) =>
        Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(valor)).ToArray();
    private static void LinhaIndicador(StringBuilder csv, string nome, DashboardDto dashboard, string categoria, decimal quantidade, string unidade) =>
        csv.Append(Campo(nome)).Append(';').Append(dashboard.Inicio.ToString("dd/MM/yyyy")).Append(';')
            .Append(dashboard.Fim.ToString("dd/MM/yyyy")).Append(';').Append(Campo(categoria)).Append(';')
            .Append(quantidade.ToString("0.###", Cultura)).Append(';').Append(Campo(unidade)).Append("\r\n");
}
