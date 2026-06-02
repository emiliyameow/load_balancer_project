using LoadBalancer.API.ServiceCache;
using Microsoft.Extensions.Options;
using System.Collections.Immutable;

namespace LoadBalancer.API.HealthCheck;

public class HealthCheckHostedService(
    IHealthChecker healthChecker,
    ServiceCacheHandler cache,
    IOptions<HealthCheckSettings> settings,
    HealthCache healthCache,
    ILogger<HealthCheckHostedService> logger)
    : BackgroundService
{
    private readonly ServiceCacheHandler _cache = cache;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(settings.Value.IntervalSeconds);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Health checker начал проверку с интервалом в {Interval} секунд", _interval.TotalSeconds);

        using var timer = new PeriodicTimer(_interval);
        await RunCheckAsync(stoppingToken);
        
        while (await timer.WaitForNextTickAsync(stoppingToken)) await RunCheckAsync(stoppingToken);
    }

    private async Task RunCheckAsync(CancellationToken ct)
    {
        try
        {
            logger.LogTrace("Начался health check цикл");
            // обновляем не кэш сервисов, а обновляем health cache
            var results = await healthChecker.CheckAllServersAsync();
            
            healthCache.Update(results);
            var allBackends = results.ToImmutableList();
            
            var snapshot = ImmutableDictionary<string, ImmutableList<ServerCondition>>
                .Empty
                .Add("users-service", allBackends);

            var alive = results.Count(r => r.IsAlive);
            logger.LogDebug("Health check completed: {Alive}/{Total} servers alive", alive, results.Count);
        }
        catch (OperationCanceledException)
        {
            // Нормальное завершение при остановке приложения
            logger.LogInformation("Health check cancelled (shutdown)");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error in health check cycle");
            // !!! Не выбрасываем исключение — иначе остановится весь фон-сервис
        }
    }
}