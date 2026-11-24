namespace Caramello.Api.Services;

public sealed class BackupAgendadoService(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<BackupAgendadoService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var horas = Math.Clamp(configuration.GetValue("Backup:IntervaloHoras", 24), 1, 168);
        using var timer = new PeriodicTimer(TimeSpan.FromHours(horas));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var backup = await scope.ServiceProvider.GetRequiredService<IBackupService>().CriarAsync(stoppingToken);
                logger.LogInformation("Backup automático concluído: {Nome}", backup.Nome);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception)
            {
                logger.LogError(exception, "Falha ao criar backup automático.");
            }
        }
    }
}
