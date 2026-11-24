using System.Diagnostics;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Caramello.Desktop;

public sealed class MainForm : Form
{
    private readonly WebView2 _webView = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly Label _status = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 13), Text = "Iniciando o Caramello Gestão Sustentável…" };
    private static readonly string PastaLocal = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CaramelloGestao");
    private readonly string _baseDados = Environment.GetEnvironmentVariable("CARAMELLO_DESKTOP_DATA_PATH") ?? PastaLocal;
    private Process? _servidor;
    private Uri? _origem;
    private LogInicializacao? _log;
    private readonly CancellationTokenSource _fechamento = new();
    private Task _inicializacao = Task.CompletedTask;
    private bool _podeFechar;
    private readonly string _shutdownToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

    public MainForm()
    {
        Text = "Caramello Gestão Sustentável";
        Width = 1280; Height = 850; MinimumSize = new Size(960, 640); StartPosition = FormStartPosition.CenterScreen;
        var menu = new MenuStrip();
        var dados = new ToolStripMenuItem("Dados");
        dados.DropDownItems.Add("Escolher pasta de backups…", null, (_, _) => EscolherPastaBackup());
        dados.DropDownItems.Add("Abrir pasta de dados", null, (_, _) => AbrirPasta(_baseDados));
        menu.Items.Add(dados);
        Controls.Add(_webView); Controls.Add(_status); Controls.Add(menu); MainMenuStrip = menu;
        Shown += (_, _) => _inicializacao = InicializarAsync();
        FormClosing += AoFechar;
    }

    private async Task InicializarAsync()
    {
        var etapa = "preparação dos diretórios e do log";
        try
        {
            try { _log = new LogInicializacao(Path.Combine(PastaLocal, "logs")); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            _log?.Registrar("Inicialização do Caramello.");
            etapa = "verificação dos arquivos e diretórios";
            var data = CriarDiretorio("data"); var chaves = CriarDiretorio("keys"); var backup = ObterPastaBackup();
            var servidor = Path.Combine(AppContext.BaseDirectory, "server", "Caramello.Api.exe");
            foreach (var arquivo in new[] { "Caramello.Api.exe", "Caramello.Api.deps.json", "Caramello.Api.runtimeconfig.json", Path.Combine("wwwroot", "index.html") })
            {
                if (!File.Exists(Path.Combine(Path.GetDirectoryName(servidor)!, arquivo)))
                {
                    _log?.Registrar($"Arquivo publicado ausente: {arquivo}.");
                    throw new FalhaInicializacao($"Um componente da instalação não foi encontrado ({arquivo}).");
                }
            }

            etapa = "migração do banco";
            _status.Text = "Preparando banco de dados com segurança…";
            using (var migracao = CriarProcesso(servidor, true, data, chaves, backup))
            {
                IniciarProcesso(migracao, etapa);
                await migracao.WaitForExitAsync();
                _log?.Registrar($"Migração encerrada com código {migracao.ExitCode}.");
                if (migracao.ExitCode != 0)
                    throw new FalhaInicializacao($"A preparação do banco falhou (código {migracao.ExitCode}).");
            }
            _fechamento.Token.ThrowIfCancellationRequested();

            etapa = "prontidão da API";
            var endereco = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);
            var encerramento = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var processo = CriarProcesso(servidor, false, data, chaves, backup);
            processo.Exited += (_, _) => encerramento.TrySetResult(processo.ExitCode);
            _servidor = processo;
            IniciarProcesso(processo, etapa, endereco);
            using (var http = CriarCliente())
            {
                _origem = await ProntidaoServidor.AguardarAsync(endereco.Task, encerramento.Task, http,
                    TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(350), _fechamento.Token);
            }
            _log?.Registrar("API pronta.");

            etapa = "inicialização do WebView2";
            _status.Text = "Carregando interface…";
            var versao = CoreWebView2Environment.GetAvailableBrowserVersionString();
            if (string.IsNullOrWhiteSpace(versao)) throw new FalhaInicializacao("O Microsoft Edge WebView2 Runtime não está instalado.");
            var ambiente = await CoreWebView2Environment.CreateAsync(null, CriarDiretorio("webview2"));
            await _webView.EnsureCoreWebView2Async(ambiente);
            _fechamento.Token.ThrowIfCancellationRequested();
            ConfigurarNavegacao();
            _webView.Source = _origem;
            _webView.Visible = true; _status.Visible = false;
            _log?.Registrar("Interface carregada.");
        }
        catch (OperationCanceledException) when (_fechamento.IsCancellationRequested) { }
        catch (Exception exception)
        {
            var motivo = exception switch
            {
                FalhaInicializacao => exception.Message,
                TimeoutException or ApiEncerradaException when etapa == "prontidão da API" => exception.Message,
                UnauthorizedAccessException => "O Windows negou acesso a um arquivo ou diretório necessário.",
                Microsoft.Web.WebView2.Core.WebView2RuntimeNotFoundException => "O Microsoft Edge WebView2 Runtime não está instalado.",
                _ => $"Falha durante a etapa de {etapa}."
            };
            var detalhes = $"{motivo}\r\nEtapa: {etapa}\r\nErro: {exception.GetType().Name} (0x{exception.HResult:X8})";
            _log?.Registrar(detalhes);
            await EncerrarServidorAsync();
            if (_fechamento.IsCancellationRequested) return;
            _status.Text = "Não foi possível iniciar o aplicativo.";
            var caminhoLog = _log?.Caminho ?? $"indisponível em {Path.Combine(PastaLocal, "logs")}";
            MostrarFalha($"{detalhes}\r\n\r\nLog: {caminhoLog}\r\n\r\nOs dados existentes foram preservados.");
        }
    }

    private Process CriarProcesso(string executavel, bool migracao, string data, string chaves, string backup)
    {
        var info = new ProcessStartInfo(executavel, migracao ? "--migrate-desktop" : string.Empty)
        {
            WorkingDirectory = Path.GetDirectoryName(executavel)!,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        info.Environment["ASPNETCORE_ENVIRONMENT"] = "Desktop";
        info.Environment["DOTNET_ENVIRONMENT"] = "Desktop";
        info.Environment["ASPNETCORE_CONTENTROOT"] = info.WorkingDirectory;
        info.Environment["DOTNET_CONTENTROOT"] = info.WorkingDirectory;
        info.Environment["CARAMELLO_DATABASE_PATH"] = Path.Combine(data, "caramello.db");
        info.Environment["CARAMELLO_KEYS_PATH"] = chaves;
        info.Environment["CARAMELLO_BACKUP_PATH"] = backup;
        info.Environment["Backup__Retencao"] = "30";
        info.Environment["Backup__IntervaloHoras"] = "24";
        if (migracao) info.Environment.Remove("CARAMELLO_DESKTOP_SHUTDOWN_TOKEN");
        else info.Environment["CARAMELLO_DESKTOP_SHUTDOWN_TOKEN"] = _shutdownToken;
        return new Process { StartInfo = info, EnableRaisingEvents = true };
    }

    private void IniciarProcesso(Process processo, string etapa, TaskCompletionSource<Uri>? endereco = null)
    {
        const string prefixo = "CARAMELLO_DESKTOP_PORT=";
        processo.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not { } linha) return;
            if (endereco is not null && linha.StartsWith(prefixo, StringComparison.Ordinal)
                && int.TryParse(linha.AsSpan(prefixo.Length), out var porta) && porta is > 0 and <= 65535)
            {
                endereco.TrySetResult(new Uri($"http://127.0.0.1:{porta}"));
                _log?.Registrar($"API vinculada ao loopback na porta {porta}.");
            }
            else _log?.RegistrarSaida(etapa, "stdout", linha);
        };
        processo.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is { } linha) _log?.RegistrarSaida(etapa, "stderr", linha);
        };
        _log?.Registrar($"Iniciando {etapa}.");
        processo.Start();
        processo.BeginOutputReadLine();
        processo.BeginErrorReadLine();
    }

    private void ConfigurarNavegacao()
    {
        var core = _webView.CoreWebView2;
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.AreHostObjectsAllowed = false;
        core.Settings.IsStatusBarEnabled = false;
        core.NavigationStarting += (_, e) =>
        {
            if (!OrigemPermitida(e.Uri)) { e.Cancel = true; AbrirLinkExterno(e.Uri); }
        };
        core.NewWindowRequested += (_, e) => { e.Handled = true; if (!OrigemPermitida(e.Uri)) AbrirLinkExterno(e.Uri); else core.Navigate(e.Uri); };
        core.PermissionRequested += (_, e) => e.State = CoreWebView2PermissionState.Deny;
    }

    private bool OrigemPermitida(string endereco) => Uri.TryCreate(endereco, UriKind.Absolute, out var uri) && _origem is not null && uri.Scheme == _origem.Scheme && uri.Host == _origem.Host && uri.Port == _origem.Port;
    private static void AbrirLinkExterno(string endereco) { if (Uri.TryCreate(endereco, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "mailto" or "tel") Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }); }
    private string CriarDiretorio(string nome) { var caminho = Path.GetFullPath(Path.Combine(_baseDados, nome)); Directory.CreateDirectory(caminho); return caminho; }
    private string ObterPastaBackup() { var configuracao = Path.Combine(_baseDados, "backup-path.txt"); var caminho = File.Exists(configuracao) ? File.ReadAllText(configuracao).Trim() : string.Empty; if (!Path.IsPathFullyQualified(caminho)) return CriarDiretorio("backups"); Directory.CreateDirectory(caminho); return caminho; }
    private void EscolherPastaBackup() { using var dialog = new FolderBrowserDialog { Description = "Escolha onde os backups serão guardados", UseDescriptionForTitle = true, SelectedPath = ObterPastaBackup() }; if (dialog.ShowDialog(this) != DialogResult.OK) return; File.WriteAllText(Path.Combine(_baseDados, "backup-path.txt"), dialog.SelectedPath); MessageBox.Show("A nova pasta será usada na próxima vez que o aplicativo for aberto.", "Pasta de backup", MessageBoxButtons.OK, MessageBoxIcon.Information); }
    private static void AbrirPasta(string caminho) { Directory.CreateDirectory(caminho); Process.Start(new ProcessStartInfo("explorer.exe", caminho) { UseShellExecute = true }); }
    private static HttpClient CriarCliente() => new(new HttpClientHandler
    {
        AllowAutoRedirect = false,
        UseProxy = false,
        UseCookies = false
    }) { Timeout = Timeout.InfiniteTimeSpan };

    private void MostrarFalha(string detalhes)
    {
        using var janela = new Form
        {
            Text = "Falha ao iniciar",
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(620, 265),
            MinimizeBox = false,
            MaximizeBox = false
        };
        var texto = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            AccessibleName = "Detalhes técnicos da inicialização",
            Text = $"O Caramello não conseguiu iniciar.\r\n\r\n{detalhes}"
        };
        var copiar = new Button { Text = "Copiar detalhes", AutoSize = true };
        copiar.Click += (_, _) =>
        {
            try { Clipboard.SetText(texto.Text); }
            catch (System.Runtime.InteropServices.ExternalException) { texto.Focus(); texto.SelectAll(); }
        };
        var fechar = new Button { Text = "Fechar", AutoSize = true, DialogResult = DialogResult.OK };
        var botoes = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true };
        botoes.Controls.AddRange([copiar, fechar]);
        janela.Controls.Add(texto);
        janela.Controls.Add(botoes);
        janela.CancelButton = fechar;
        janela.ShowDialog(this);
    }

    private async void AoFechar(object? sender, FormClosingEventArgs e)
    {
        if (_podeFechar) return;
        e.Cancel = true;
        if (_fechamento.IsCancellationRequested) return;
        _status.Text = "Encerrando o Caramello…";
        await _fechamento.CancelAsync();
        await _inicializacao;
        await EncerrarServidorAsync();
        _fechamento.Dispose();
        _podeFechar = true;
        BeginInvoke(Close);
    }

    private async Task EncerrarServidorAsync()
    {
        using var servidor = _servidor;
        _servidor = null;
        if (servidor is null) return;
        try
        {
            if (!servidor.HasExited && _origem is not null)
            {
                using var http = CriarCliente();
                using var prazo = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_origem, "/api/desktop/shutdown"));
                request.Headers.Add("X-Caramello-Desktop-Token", _shutdownToken);
                try
                {
                    using var resposta = await http.SendAsync(request, prazo.Token);
                    await servidor.WaitForExitAsync(prazo.Token);
                }
                catch (HttpRequestException) { }
                catch (OperationCanceledException) { }
            }
            if (!servidor.HasExited) servidor.Kill(entireProcessTree: true);
            using var espera = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await servidor.WaitForExitAsync(espera.Token);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or OperationCanceledException)
        {
            _log?.Registrar($"Encerramento da API: {ex.GetType().Name} (0x{ex.HResult:X8}).");
        }
    }

    private sealed class FalhaInicializacao(string mensagem) : InvalidOperationException(mensagem);
}
