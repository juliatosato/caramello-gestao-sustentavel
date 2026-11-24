using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Caramello.Desktop;

internal static class ProntidaoServidor
{
    public static async Task<Uri> AguardarAsync(
        Task<Uri> endereco, Task<int> encerramento, HttpClient http,
        TimeSpan limite, TimeSpan limiteTentativa, TimeSpan intervalo, CancellationToken cancelamento)
    {
        using var prazo = CancellationTokenSource.CreateLinkedTokenSource(cancelamento);
        prazo.CancelAfter(limite);
        using var sondagem = CancellationTokenSource.CreateLinkedTokenSource(prazo.Token);
        var verificacao = VerificarAsync();
        if (await Task.WhenAny(verificacao, encerramento) == encerramento || encerramento.IsCompletedSuccessfully)
        {
            await sondagem.CancelAsync();
            try { await verificacao; }
            catch (OperationCanceledException) { }
            catch (TimeoutException) { }
            cancelamento.ThrowIfCancellationRequested();
            throw new ApiEncerradaException(await encerramento);
        }
        return await verificacao;

        async Task<Uri> VerificarAsync()
        {
            HttpStatusCode? ultimoErro = null;
            try
            {
                var origem = await endereco.WaitAsync(sondagem.Token);
                var health = new Uri(origem, "/api/health");
                while (true)
                {
                    using var tentativa = CancellationTokenSource.CreateLinkedTokenSource(sondagem.Token);
                    tentativa.CancelAfter(limiteTentativa);
                    try
                    {
                        using var resposta = await http.GetAsync(health, HttpCompletionOption.ResponseHeadersRead, tentativa.Token);
                        ultimoErro = resposta.StatusCode;
                        if (resposta.StatusCode == HttpStatusCode.OK
                            && resposta.Content.Headers.ContentType?.MediaType == "application/json"
                            && (await resposta.Content.ReadFromJsonAsync<RespostaHealth>(tentativa.Token))?.Status == "ok")
                            return origem;
                    }
                    catch (OperationCanceledException) when (!sondagem.IsCancellationRequested) { }
                    catch (HttpRequestException) { }
                    catch (JsonException) { }
                    await Task.Delay(intervalo, sondagem.Token);
                }
            }
            catch (OperationCanceledException) when (prazo.IsCancellationRequested && !cancelamento.IsCancellationRequested)
            {
                var mensagem = ultimoErro switch
                {
                    HttpStatusCode.OK => "A API retornou uma resposta inválida na verificação de prontidão.",
                    { } status => $"A API retornou HTTP {(int)status} na verificação de prontidão.",
                    _ => "A API não respondeu à verificação de prontidão dentro do prazo."
                };
                throw new TimeoutException(mensagem);
            }
        }
    }

    private sealed record RespostaHealth(string Status);
}

internal sealed class ApiEncerradaException(int codigo)
    : InvalidOperationException($"A API encerrou inesperadamente (código {codigo}).");
