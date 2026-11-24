using System.Text;
using Caramello.Api.Domain;
using Caramello.Api.Dtos;
using Caramello.Api.Infrastructure;
using Caramello.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Caramello.Api.Tests;

public class IndicadoresIntegracaoTests
{
    [Fact]
    public async Task Dashboard_SeparaTotaisPorUnidadeETipo()
    {
        await using var contexto = await CriarContextoAsync();
        var hoje = DateTime.UtcNow;
        var kg = NovoInsumo("Teste kg", "Alimentos", "kg", 10);
        var litros = NovoInsumo("Teste L", "Bebidas", "L", 10);
        contexto.Insumos.AddRange(kg, litros);
        await contexto.SaveChangesAsync();
        contexto.MovimentacoesEstoque.AddRange(
            Movimento(kg, TipoMovimentacao.Consumo, 2, hoje, "Preparo"),
            Movimento(kg, TipoMovimentacao.Descarte, 1, hoje, "Validade"),
            Movimento(litros, TipoMovimentacao.Descarte, 3, hoje, "Avaria"));
        await contexto.SaveChangesAsync();

        var resultado = await new IndicadoresService(contexto).ObterDashboardAsync(hoje, hoje, CancellationToken.None);

        Assert.Equal(2, resultado.Descartes.Count);
        Assert.Equal(1, resultado.Descartes.Single(x => x.Unidade == "kg").Quantidade);
        Assert.Equal(3, resultado.Descartes.Single(x => x.Unidade == "L").Quantidade);
        Assert.Equal(2, resultado.Consumo.Single().Quantidade);
    }

    [Fact]
    public async Task Analise_CalculaProporcaoSemDuplicarDescartesAmbientais()
    {
        await using var contexto = await CriarContextoAsync();
        var hoje = DateTime.UtcNow;
        var insumo = NovoInsumo("Teste", "Alimentos", "kg", 10);
        contexto.Insumos.Add(insumo);
        contexto.Descartes.Add(new Descarte { Material = "Outro registro", Unidade = "kg", Quantidade = 50, Data = hoje });
        await contexto.SaveChangesAsync();
        contexto.MovimentacoesEstoque.AddRange(
            Movimento(insumo, TipoMovimentacao.Consumo, 3, hoje, "Preparo"),
            Movimento(insumo, TipoMovimentacao.Descarte, 1, hoje, "Validade"));
        await contexto.SaveChangesAsync();

        var resultado = await new IndicadoresService(contexto).AnalisarDesperdicioAsync(
            hoje, hoje, "Alimentos", "Validade", CancellationToken.None);

        var item = Assert.Single(resultado.Insumos);
        Assert.Equal(1, item.QuantidadeDescartada);
        Assert.Equal(4, item.SaidasTotais);
        Assert.Equal(25, item.ProporcaoDescarte);
    }

    [Fact]
    public async Task Sugestao_UsaConsumoMedioPrazoSegurancaESaldo()
    {
        await using var contexto = await CriarContextoAsync();
        var fim = DateTime.UtcNow.Date;
        var insumo = NovoInsumo("Teste", "Alimentos", "kg", 2);
        insumo.PrazoReposicaoDias = 5;
        insumo.EstoqueSeguranca = 1;
        contexto.Insumos.Add(insumo);
        await contexto.SaveChangesAsync();
        contexto.MovimentacoesEstoque.AddRange(
            Movimento(insumo, TipoMovimentacao.Consumo, 4, fim.AddDays(-7), "Preparo"),
            Movimento(insumo, TipoMovimentacao.Consumo, 4, fim, "Preparo"));
        await contexto.SaveChangesAsync();

        var resultado = await new IndicadoresService(contexto).ObterSugestoesAsync(fim, 30, CancellationToken.None);

        var sugestao = Assert.Single(resultado);
        Assert.True(sugestao.HistoricoSuficiente);
        Assert.Equal(1, sugestao.ConsumoMedioDiario);
        Assert.Equal(4, sugestao.QuantidadeSugerida);
    }

    [Fact]
    public async Task Csv_UsaBomSeparadorEFormatoBrasileiro()
    {
        await using var contexto = await CriarContextoAsync();
        var hoje = DateTime.UtcNow;
        var insumo = NovoInsumo("Teste", "Categoria", "kg", 1);
        contexto.Insumos.Add(insumo);
        await contexto.SaveChangesAsync();
        contexto.MovimentacoesEstoque.Add(Movimento(insumo, TipoMovimentacao.Consumo, 1.250m, hoje, "Teste"));
        await contexto.SaveChangesAsync();

        var arquivo = await new IndicadoresService(contexto).ExportarMovimentacoesCsvAsync(hoje, hoje, CancellationToken.None);
        var texto = Encoding.UTF8.GetString(arquivo);

        Assert.Equal([0xEF, 0xBB, 0xBF], arquivo[..3]);
        Assert.Contains("Data;Insumo;Categoria;Tipo;Quantidade;Unidade;Motivo", texto);
        Assert.Contains("1,25", texto);
    }

    private static Insumo NovoInsumo(string nome, string categoria, string unidade, decimal saldo) => new()
    {
        Nome = nome,
        Categoria = categoria,
        Unidade = unidade,
        Quantidade = saldo
    };

    private static MovimentacaoEstoque Movimento(
        Insumo insumo,
        TipoMovimentacao tipo,
        decimal quantidade,
        DateTime data,
        string motivo) => new()
    {
        InsumoId = insumo.Id,
        Tipo = tipo,
        Quantidade = quantidade,
        Unidade = insumo.Unidade,
        Data = data,
        Motivo = motivo
    };

    private static async Task<AppDbContext> CriarContextoAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var contexto = new AppDbContext(options);
        await contexto.Database.EnsureCreatedAsync();
        return contexto;
    }
}
