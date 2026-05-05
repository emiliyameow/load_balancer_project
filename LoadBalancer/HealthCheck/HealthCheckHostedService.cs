using LoadBalancer.API.ServiceCache;
using Microsoft.Extensions.Options;
using System.Collections.Immutable;

namespace LoadBalancer.API.HealthCheck;

public class HealthCheckHostedService : BackgroundService
{
    private readonly IHealthChecker _healthChecker;
    private readonly ServiceCacheHandler _cache;
    private readonly ILogger<HealthCheckHostedService> _logger;
    private readonly TimeSpan _interval;
    private readonly HealthCache _healthCache;
    
    public HealthCheckHostedService(
        IHealthChecker healthChecker,
        ServiceCacheHandler cache,
        IOptions<HealthCheckSettings> settings,
        HealthCache healthCache,
        ILogger<HealthCheckHostedService> logger)
    {
        _healthChecker = healthChecker;
        _cache = cache;
        _logger = logger;
        _interval = TimeSpan.FromSeconds(settings.Value.IntervalSeconds);
        _healthCache = healthCache;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Health checker начал проверку с интервалом в {Interval} секунд", _interval.TotalSeconds);

        using var timer = new PeriodicTimer(_interval);

        // Опционально: первая проверка сразу при старте
        await RunCheckAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCheckAsync(stoppingToken);
        }
    }

    private async Task RunCheckAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogTrace("Начался health check цикл");
            // обновляем не кэш сервисов, а обновляем health cache
            var results = await _healthChecker.CheckAllServersAsync();
            
            _healthCache.Update(results);
            var allBackends = results.ToImmutableList();
            
            var snapshot = ImmutableDictionary<string, ImmutableList<ServerCondition>>
                .Empty
                .Add("users-service", allBackends);

            var alive = results.Count(r => r.IsAlive);
            _logger.LogDebug("Health check completed: {Alive}/{Total} servers alive",
                alive, results.Count);
            Console.WriteLine($"Health check completed: {alive}/{results.Count} servers alive");
        }
        catch (OperationCanceledException)
        {
            // Нормальное завершение при остановке приложения
            _logger.LogInformation("Health check cancelled (shutdown)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in health check cycle");
            // !!! Не выбрасываем исключение — иначе остановится весь фон-сервис
        }
    }
}