using Microsoft.Data.Sqlite;

namespace Caramello.Api.Services;

public sealed record BackupInfo(string Nome, long TamanhoBytes, DateTime CriadoUtc);

public interface IBackupService
{
    Task<BackupInfo> CriarAsync(CancellationToken ct);
    IReadOnlyList<BackupInfo> Listar();
}

public sealed class BackupService(IConfiguration configuration, IWebHostEnvironment environment) : IBackupService
{
    public async Task<BackupInfo> CriarAsync(CancellationToken ct)
    {
        var origem = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("A conexão do banco não foi configurada.");
        var diretorio = Environment.GetEnvironmentVariable("CARAMELLO_BACKUP_PATH")
            ?? Path.Combine(environment.ContentRootPath, "backups");
        if (!Path.IsPathRooted(diretorio)) diretorio = Path.GetFullPath(diretorio, environment.ContentRootPath);
        Directory.CreateDirectory(diretorio);

        var destino = Path.Combine(diretorio, $"caramello-{DateTime.UtcNow:yyyyMMdd-HHmmss}.db");
        await using var conexaoOrigem = new SqliteConnection(origem);
        await using var conexaoDestino = new SqliteConnection($"Data Source={destino}");
        await conexaoOrigem.OpenAsync(ct);
        await conexaoDestino.OpenAsync(ct);
        conexaoOrigem.BackupDatabase(conexaoDestino);

        AplicarRetencao(diretorio, Math.Clamp(configuration.GetValue("Backup:Retencao", 10), 1, 100));
        return Mapear(new FileInfo(destino));
    }

    public IReadOnlyList<BackupInfo> Listar()
    {
        var diretorio = Environment.GetEnvironmentVariable("CARAMELLO_BACKUP_PATH")
            ?? Path.Combine(environment.ContentRootPath, "backups");
        if (!Path.IsPathRooted(diretorio)) diretorio = Path.GetFullPath(diretorio, environment.ContentRootPath);
        if (!Directory.Exists(diretorio)) return [];
        return Directory.EnumerateFiles(diretorio, "caramello-*.db")
            .Select(x => new FileInfo(x)).OrderByDescending(x => x.CreationTimeUtc).Select(Mapear).ToList();
    }

    private static BackupInfo Mapear(FileInfo arquivo) => new(arquivo.Name, arquivo.Length, arquivo.CreationTimeUtc);

    private static void AplicarRetencao(string diretorio, int retencao)
    {
        foreach (var arquivo in Directory.EnumerateFiles(diretorio, "caramello-*.db")
                     .Select(x => new FileInfo(x)).OrderByDescending(x => x.CreationTimeUtc).Skip(retencao))
            arquivo.Delete();
    }
}
