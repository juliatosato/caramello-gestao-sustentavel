namespace Caramello.Desktop;

internal sealed class LogInicializacao
{
    private readonly object _bloqueio = new();
    private int _linhas;
    public string Caminho { get; }

    public LogInicializacao(string pasta)
    {
        Directory.CreateDirectory(pasta);
        Caminho = Path.Combine(pasta, $"inicializacao-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.log");
        File.WriteAllText(Caminho, string.Empty);
        foreach (var antigo in new DirectoryInfo(pasta).GetFiles("inicializacao-*.log")
                     .Where(arquivo => arquivo.FullName != Caminho)
                     .OrderByDescending(arquivo => arquivo.LastWriteTimeUtc).Skip(9))
        {
            try { antigo.Delete(); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    // A saída da API pode conter SQL, identificadores, caminhos e valores de configuração.
    // Persiste apenas diagnósticos de um catálogo fixo, sem copiar trechos da saída.
    public void RegistrarSaida(string etapa, string fluxo, string linha)
    {
        var diagnostico = linha switch
        {
            _ when linha.Contains("SQLite Error 5:", StringComparison.Ordinal) => "SQLite ocupado (código 5).",
            _ when linha.Contains("SQLite Error 8:", StringComparison.Ordinal) => "SQLite somente leitura (código 8).",
            _ when linha.Contains("SQLite Error 14:", StringComparison.Ordinal) => "SQLite não conseguiu abrir o arquivo (código 14).",
            _ when linha.Contains("SqliteException", StringComparison.Ordinal) => "Falha do SQLite.",
            _ when linha.Contains("Applying migration", StringComparison.Ordinal) => "Aplicando migration do EF Core.",
            _ when linha.Contains("UnauthorizedAccessException", StringComparison.Ordinal) => "Permissão de acesso negada.",
            _ when linha.Contains("Cannot open log for source", StringComparison.Ordinal) => "Falha de permissão no Windows Event Log.",
            _ when linha.Contains("FileNotFoundException", StringComparison.Ordinal) => "Arquivo ou dependência ausente.",
            _ when linha.Contains("DllNotFoundException", StringComparison.Ordinal) => "Biblioteca nativa ausente.",
            _ when linha.Contains("AddressInUseException", StringComparison.Ordinal) => "Falha ao vincular a porta local.",
            _ when linha.Contains("DataProtection", StringComparison.Ordinal) => "Evento do Data Protection.",
            _ when linha.Contains("variáveis de criação do administrador", StringComparison.Ordinal)
                || linha.Contains("Remova as variáveis temporárias", StringComparison.Ordinal) => "Revise as variáveis temporárias do administrador.",
            _ when linha.Contains("Unhandled exception", StringComparison.Ordinal) => "Exceção não tratada na API.",
            _ when linha.StartsWith("fail:", StringComparison.Ordinal) => "A API registrou uma falha.",
            _ when linha.StartsWith("warn:", StringComparison.Ordinal) => "A API registrou um aviso.",
            _ => null
        };
        if (diagnostico is not null) Registrar($"{etapa} ({fluxo}): {diagnostico}", saidaApi: true);
    }

    public void Registrar(string mensagem, bool saidaApi = false)
    {
        lock (_bloqueio)
        {
            if (saidaApi && _linhas++ >= 1000) return;
            try { File.AppendAllText(Caminho, $"{DateTimeOffset.Now:O} {mensagem}{Environment.NewLine}"); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
