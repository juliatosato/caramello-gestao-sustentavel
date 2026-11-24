using Caramello.Api.Dtos;
using Caramello.Api.Services;
using Xunit;

namespace Caramello.Api.Tests;

public class ValidacaoTests
{
    [Fact]
    public void InsumoValido_NaoRetornaErro()
    {
        var input = new InsumoInput("Café", "Bebidas", "kg", 2, 1);

        Assert.Null(Validacao.Validar(input));
    }

    [Theory]
    [InlineData("", "Bebidas", "kg", 1, 0)]
    [InlineData("Café", "", "kg", 1, 0)]
    [InlineData("Café", "Bebidas", "kg", -1, 0)]
    [InlineData("Café", "Bebidas", "caixa", 1, 0)]
    public void InsumoInvalido_RetornaErro(string nome, string categoria, string unidade, decimal quantidade, decimal minimo)
    {
        var input = new InsumoInput(nome, categoria, unidade, quantidade, minimo);

        Assert.NotNull(Validacao.Validar(input));
    }

    [Fact]
    public void DescarteComQuantidadeZero_RetornaErro()
    {
        var input = new DescarteInput("Borra de café", "kg", 0, null);

        Assert.Equal("A quantidade deve ser maior que zero.", Validacao.Validar(input));
    }

    [Fact]
    public void DescarteValido_NaoRetornaErro()
    {
        var input = new DescarteInput("Borra de café", "kg", 0.5m, "Compostagem");

        Assert.Null(Validacao.Validar(input));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.0001)]
    public void MovimentacaoComQuantidadeInvalida_RetornaErro(decimal quantidade)
    {
        var input = new MovimentacaoInput(Caramello.Api.Domain.TipoMovimentacao.Entrada, quantidade, null);

        Assert.NotNull(Validacao.Validar(input));
    }

    [Fact]
    public void BorraDestinadaSemDestinacao_RetornaErro()
    {
        var input = new RegistroBorraInput(
            Caramello.Api.Domain.TipoRegistroBorra.Destinada,
            1,
            "kg",
            null,
            null,
            DateTime.Now);

        Assert.Equal("Informe a forma de destinação.", Validacao.Validar(input));
    }
}
