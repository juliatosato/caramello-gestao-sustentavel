using Caramello.Api.Domain;
using Caramello.Api.Infrastructure;
using Caramello.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Caramello.Api.Tests;

public sealed class AuditoriaBackupTests
{
    [Fact]
    public async Task CriacaoDeInsumoGeraAuditoriaSemDadosSensiveis()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options); await db.Database.EnsureCreatedAsync();
        db.Insumos.Add(new Insumo { Nome = "Item de teste", Categoria = "Teste", Unidade = "kg" });
        await db.SaveChangesAsync();
        var registro = await db.RegistrosAuditoria.SingleAsync();
        Assert.Equal("Criação", registro.Operacao);
        Assert.Equal(nameof(Insumo), registro.Entidade);
        Assert.DoesNotContain("senha", registro.IdentificadorRegistro, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BackupSqlitePodeSerAbertoERestaurado()
    {
        var raiz = Path.Combine(Path.GetTempPath(), $"caramello-backup-test-{Guid.NewGuid():N}"); Directory.CreateDirectory(raiz);
        try
        {
            var banco = Path.Combine(raiz, "origem.db"); var backups = Path.Combine(raiz, "backups");
            await using (var db = new SqliteConnection($"Data Source={banco}")) { await db.OpenAsync(); var cmd = db.CreateCommand(); cmd.CommandText = "CREATE TABLE Teste (Id INTEGER PRIMARY KEY); INSERT INTO Teste DEFAULT VALUES;"; await cmd.ExecuteNonQueryAsync(); }
            Environment.SetEnvironmentVariable("CARAMELLO_BACKUP_PATH", backups);
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Database"] = $"Data Source={banco}", ["Backup:Retencao"] = "2" }).Build();
            var service = new BackupService(config, new AmbienteTeste(raiz));
            var backup = await service.CriarAsync(CancellationToken.None);
            await using var restaurado = new SqliteConnection($"Data Source={Path.Combine(backups, backup.Nome)}"); await restaurado.OpenAsync();
            var consulta = restaurado.CreateCommand(); consulta.CommandText = "SELECT COUNT(*) FROM Teste";
            Assert.Equal(1L, (long)(await consulta.ExecuteScalarAsync())!);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CARAMELLO_BACKUP_PATH", null);
            try { Directory.Delete(raiz, true); } catch (IOException) { }
        }
    }

    private sealed class AmbienteTeste(string raiz) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Caramello.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = raiz;
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = raiz;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
