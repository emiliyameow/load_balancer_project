
using System.Collections.Immutable;
using LoadBalancer.API.HealthCheck;
using LoadBalancer.API.ServiceCache;

namespace LoadBalancer.API.ServiceDiscovery;

/// <summary>
/// Фоновый сервис, который периодически обновляет список сервисов,
/// вычисляет diff и применяет изменения в кэш (incremental update).
/// </summary>
public class ServiceDiscoveryUpdater : BackgroundService
{
    private readonly IServiceRegistry _registry;
    private readonly ServiceCacheHandler _cache;
    private readonly ILogger<ServiceDiscoveryUpdater> _logger;
    
    private readonly TimeSpan _maxBackoff = TimeSpan.FromSeconds(30);

    public ServiceDiscoveryUpdater(
        IServiceRegistry registry,
        ServiceCacheHandler cache,
        ILogger<ServiceDiscoveryUpdater> logger)
    {
        _registry = registry;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Основной цикл: запускает sync и повторяет его с backoff.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int attempt = 0;

        attempt = await SafeSync(stoppingToken, attempt);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CalculateDelay(attempt);

            await Task.Delay(delay, stoppingToken);

            attempt = await SafeSync(stoppingToken, attempt);
        }
    }

    /// <summary>
    /// Обёртка над Sync с обработкой ошибок и retry-счётчиком.
    /// </summary>
    private async Task<int> SafeSync(CancellationToken ct, int attempt)
    {
        try
        {
            await SyncAsync(ct);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Service discovery failed (attempt {Attempt})",
                attempt + 1);

            return attempt + 1;
        }
    }
    
    /// <summary>
    /// Получает сервисы из registry, строит snapshot и применяет diff.
    /// </summary>
    private async Task SyncAsync(CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        var services = await _registry.GetServicesAsync();

        var snapshot = BuildSnapshot(services);
        
        ApplyDiff(snapshot);

        var totalInstances = snapshot.Sum(s => s.Value.Count);

        _logger.LogInformation(
            "Service cache updated: {Services} services, {Instances} instances",
            snapshot.Count,
            totalInstances);
    }
    
    /// <summary>
    /// Сравнивает старый и новый snapshot и определяет diff по сервисам.
    /// </summary>
    private void ApplyDiff(
        ImmutableDictionary<string, ImmutableList<ServerCondition>> newSnapshot)
    {
        var oldSnapshot = _cache.GetAll();

        var allServices = oldSnapshot.Keys
            .Union(newSnapshot.Keys);

        foreach (var service in allServices)
        {
            oldSnapshot.TryGetValue(service, out var oldInstances);
            newSnapshot.TryGetValue(service, out var newInstances);

            oldInstances ??= ImmutableList<ServerCondition>.Empty;
            newInstances ??= ImmutableList<ServerCondition>.Empty;

            var oldMap = oldInstances.ToDictionary(x => x.ServerInfo.Address);
            var newMap = newInstances.ToDictionary(x => x.ServerInfo.Address);

            var added = newMap.Keys.Except(oldMap.Keys);
            var removed = oldMap.Keys.Except(newMap.Keys);

            var updated = newMap.Keys
                .Intersect(oldMap.Keys)
                .Where(k => !AreEqual(oldMap[k], newMap[k]));

            if (added.Any() || removed.Any() || updated.Any())
            {
                ApplyServicePatch(service, oldInstances, newInstances, added, removed, updated);
            }
        }
    }

    /// <summary>
    /// Применяет изменения для конкретного сервиса (add/remove/update).
    /// </summary>
    private void ApplyServicePatch(
        string service,
        ImmutableList<ServerCondition> oldInstances,
        ImmutableList<ServerCondition> newInstances,
        IEnumerable<string> added,
        IEnumerable<string> removed,
        IEnumerable<string> updated)
    {
        LogDiff(service, added, removed, updated);

        var result = oldInstances.ToDictionary(x => x.ServerInfo.Address);

        foreach (var r in removed)
            result.Remove(r);

        foreach (var a in added)
        {
            var instance = newInstances.First(x => x.ServerInfo.Address == a);
            result[a] = instance;
        }

        foreach (var u in updated)
        {
            var instance = newInstances.First(x => x.ServerInfo.Address == u);
            result[u] = instance;
        }

        _cache.AddOrUpdateService(
            service,
            result.Values.ToImmutableList());
    }
    
    /// <summary>
    /// Преобразует mutable структуру в immutable snapshot.
    /// </summary>
    private ImmutableDictionary<string, ImmutableList<ServerCondition>> BuildSnapshot(
        Dictionary<string, List<ServerCondition>> services)
    {
        return services.ToImmutableDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToImmutableList()
        );
    }
    
    /// <summary>
    /// Проверяет равенство двух инстансов (для diff).
    /// </summary>
    private bool AreEqual(ServerCondition a, ServerCondition b)
    {
        return a.ServerInfo.Address == b.ServerInfo.Address &&
               a.Weight == b.Weight;
    }

    /// <summary>
    /// Вычисляет задержку с exponential backoff и jitter.
    /// </summary>
    private TimeSpan CalculateDelay(int attempt)
    {
        var backoffSeconds = Math.Min(
            _maxBackoff.TotalSeconds,
            Math.Pow(2, attempt));
        
        var jitterMs = Random.Shared.Next(0, 500);

        return TimeSpan.FromSeconds(backoffSeconds) +
               TimeSpan.FromMilliseconds(jitterMs);
    }
    
    /// <summary>
    /// Логирует изменения (added/removed/updated) для сервиса.
    /// </summary>
    private void LogDiff(
        string service,
        IEnumerable<string> added,
        IEnumerable<string> removed,
        IEnumerable<string> updated)
    {
        foreach (var a in added)
            _logger.LogInformation("Service {Service}: added {Instance}", service, a);

        foreach (var r in removed)
            _logger.LogInformation("Service {Service}: removed {Instance}", service, r);

        foreach (var u in updated)
            _logger.LogInformation("Service {Service}: updated {Instance}", service, u);
    }
}