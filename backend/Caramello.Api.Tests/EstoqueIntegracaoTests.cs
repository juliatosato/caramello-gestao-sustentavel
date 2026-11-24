using Caramello.Api.Domain;
using Caramello.Api.Dtos;
using Caramello.Api.Infrastructure;
using Caramello.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;

namespace Caramello.Api.Tests;

public class EstoqueIntegracaoTests
{
    [Fact]
    public async Task ExcluirItem_DesativaEPreservaHistorico()
    {
        await using var contexto = await CriarContextoAsync();
        var service = new InsumoService(contexto);
        var insumo = await service.CriarAsync(
            new InsumoInput("Insumo de teste", "Categoria de teste", "kg", 2, 1),
            CancellationToken.None);

        var removido = await service.DesativarAsync(insumo.Id, CancellationToken.None);

        Assert.True(removido);
        Assert.Empty(await service.ListarAsync(CancellationToken.None));
        Assert.Single(await service.ListarMovimentacoesAsync(insumo.Id, CancellationToken.None));
    }

    [Fact]
    public async Task RespostaDoCadastro_NaoSerializaNavegacoesCirculares()
    {
        await using var contexto = await CriarContextoAsync();
        var service = new InsumoService(contexto);
        var insumo = await service.CriarAsync(
            new InsumoInput("Insumo de teste", "Categoria de teste", "kg", 2, 1),
            CancellationToken.None);

        var json = JsonSerializer.Serialize(InsumoDto.Criar(insumo));

        Assert.Contains("\"Nome\":\"Insumo de teste\"", json);
        Assert.DoesNotContain("Movimentacoes", json);
    }

    [Fact]
    public async Task Entrada_AtualizaSaldoERegistraHistorico()
    {
        await using var contexto = await CriarContextoAsync();
        var service = new InsumoService(contexto);
        var insumo = await service.CriarAsync(
            new InsumoInput("Insumo de teste", "Categoria de teste", "kg", 2, 1),
            CancellationToken.None);

        await service.MovimentarAsync(
            insumo.Id,
            new MovimentacaoInput(TipoMovimentacao.Entrada, 1.250m, "Entrada de teste"),
            CancellationToken.None);

        var atualizado = await service.ObterAsync(insumo.Id, CancellationToken.None);
        var historico = await service.ListarMovimentacoesAsync(insumo.Id, CancellationToken.None);
        Assert.Equal(3.250m, atualizado!.Quantidade);
        Assert.Equal(2, historico.Count);
    }

    [Theory]
    [InlineData(TipoMovimentacao.Consumo)]
    [InlineData(TipoMovimentacao.Descarte)]
    public async Task Saida_AtualizaSaldo(TipoMovimentacao tipo)
    {
        await using var contexto = await CriarContextoAsync();
        var service = new InsumoService(contexto);
        var insumo = await service.CriarAsync(
            new InsumoInput("Insumo de teste", "Categoria de teste", "un", 10, 2),
            CancellationToken.None);

        await service.MovimentarAsync(
            insumo.Id,
            new MovimentacaoInput(tipo, 3, "Saída de teste"),
            CancellationToken.None);

        var atualizado = await service.ObterAsync(insumo.Id, CancellationToken.None);
        Assert.Equal(7, atualizado!.Quantidade);
    }

    [Fact]
    public async Task SaidaSuperiorAoSaldo_EBloqueadaSemAlterarDados()
    {
        await using var contexto = await CriarContextoAsync();
        var service = new InsumoService(contexto);
        var insumo = await service.CriarAsync(
            new InsumoInput("Insumo de teste", "Categoria de teste", "L", 2, 1),
            CancellationToken.None);

        await Assert.ThrowsAsync<RegraNegocioException>(() => service.MovimentarAsync(
            insumo.Id,
            new MovimentacaoInput(TipoMovimentacao.Consumo, 3, "Consumo de teste"),
            CancellationToken.None));

        var atualizado = await service.ObterAsync(insumo.Id, CancellationToken.None);
        var historico = await service.ListarMovimentacoesAsync(insumo.Id, CancellationToken.None);
        Assert.Equal(2, atualizado!.Quantidade);
        Assert.Single(historico);
    }

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
