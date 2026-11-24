using System.Net;
using System.Net.Http.Json;
using Caramello.Desktop;
using Xunit;

namespace Caramello.Api.Tests;

public sealed class ProntidaoServidorTests
{
    private static readonly Uri Origem = new("http://127.0.0.1:50001/");
    private readonly TaskCompletionSource<int> _encerramento = new();

    [Fact]
    public async Task RepeteAposTimeoutIndividualEmInicializacaoLenta()
    {
        var tentativas = 0;
        using var http = Cliente(async ct =>
        {
            if (++tentativas == 1) await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return HttpStatusCode.OK;
        });

        Assert.Equal(Origem, await Aguardar(http));
        Assert.Equal(2, tentativas);
    }

    [Fact]
    public async Task InformaCodigoQuandoApiEncerraAntesDePublicarPorta()
    {
        using var http = Cliente(_ => throw new InvalidOperationException("Sondagem indevida"));
        var endereco = new TaskCompletionSource<Uri>();
        var espera = ProntidaoServidor.AguardarAsync(endereco.Task, _encerramento.Task, http,
            TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1), TimeSpan.Zero, default);
        _encerramento.SetResult(17);

        var erro = await Assert.ThrowsAsync<ApiEncerradaException>(() => espera);
        Assert.Contains("17", erro.Message);
    }

    [Fact]
    public async Task HealthIndisponivelInformaStatusHttpAoExpirar()
    {
        using var http = Cliente(_ => Task.FromResult(HttpStatusCode.ServiceUnavailable));

        var erro = await Assert.ThrowsAsync<TimeoutException>(() => Aguardar(http, 250));
        Assert.Contains("HTTP 503", erro.Message);
    }

    [Fact]
    public async Task InformaAusenciaDeRespostaAoExpirar()
    {
        using var http = Cliente(_ => throw new HttpRequestException("Conexão recusada"));

        var erro = await Assert.ThrowsAsync<TimeoutException>(() => Aguardar(http, 250));
        Assert.Contains("não respondeu", erro.Message);
    }

    [Fact]
    public async Task NaoConfundePaginaHtmlComHealthDisponivel()
    {
        using var http = new HttpClient(new Sondagem(_ => Task.FromResult(HttpStatusCode.OK), html: true));

        var erro = await Assert.ThrowsAsync<TimeoutException>(() => Aguardar(http, 250));
        Assert.Contains("resposta inválida", erro.Message);
    }

    [Fact]
    public async Task FicaProntoAposFalhaDeConexaoEStatusHttpDeErro()
    {
        var tentativas = 0;
        using var http = Cliente(_ => ++tentativas switch
        {
            1 => throw new HttpRequestException("Conexão recusada"),
            2 => Task.FromResult(HttpStatusCode.ServiceUnavailable),
            _ => Task.FromResult(HttpStatusCode.OK)
        });

        Assert.Equal(Origem, await Aguardar(http));
        Assert.Equal(3, tentativas);
    }

    private Task<Uri> Aguardar(HttpClient http, int limiteMs = 5000) =>
        ProntidaoServidor.AguardarAsync(Task.FromResult(Origem), _encerramento.Task, http,
            TimeSpan.FromMilliseconds(limiteMs), TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(10), default);

    private static HttpClient Cliente(Func<CancellationToken, Task<HttpStatusCode>> responder) =>
        new(new Sondagem(responder)) { Timeout = Timeout.InfiniteTimeSpan };

    private sealed class Sondagem(Func<CancellationToken, Task<HttpStatusCode>> responder, bool html = false) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("/api/health", request.RequestUri?.AbsolutePath);
            return new HttpResponseMessage(await responder(cancellationToken))
            {
                Content = html
                    ? new StringContent("<html></html>", System.Text.Encoding.UTF8, "text/html")
                    : JsonContent.Create(new { status = "ok" })
            };
        }
    }
}
