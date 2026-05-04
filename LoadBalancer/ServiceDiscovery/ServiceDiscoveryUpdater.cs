
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
    
    private readonly TimeSpan _maxBackoff = TimeSpan.FromSeconds(1);
    private readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(2);
    private readonly int _maxRetryAttempts = 10;
    private readonly int _jitterMaxMs = 500;
    
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
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Service discovery timeout");
            return Math.Min(attempt + 1, _maxRetryAttempts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Service discovery failed (attempt {Attempt})",
                attempt + 1);

            return Math.Min(attempt + 1, _maxRetryAttempts);
        }
    }

    /// <summary>
    /// Получает сервисы из registry, строит snapshot и применяет diff.
    /// </summary>
    private async Task SyncAsync(CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_requestTimeout);

        var services = await _registry.GetServicesAsync(cts.Token);

        var snapshot = BuildSnapshot(services);

        await ApplyDiffAsync(snapshot, cts.Token);

        var totalInstances = snapshot.Sum(s => s.Value.Count);

        _logger.LogInformation(
            "Service cache updated: {Services} services, {Instances} instances",
            snapshot.Count,
            totalInstances);
    }
    /// <summary>
    /// Сравнивает старый и новый snapshot и определяет diff по сервисам.
    /// </summary>
    private async Task ApplyDiffAsync(
    ImmutableDictionary<string, ImmutableList<ServerCondition>> newSnapshot,
    CancellationToken ct)
    {
        var oldSnapshot = _cache.GetAll();

        var updates = new Dictionary<string, ImmutableList<ServerCondition>>();

        foreach (var (service, newInstances) in newSnapshot)
        {
            ct.ThrowIfCancellationRequested();

            oldSnapshot.TryGetValue(service, out var oldInstances);
            oldInstances ??= ImmutableList<ServerCondition>.Empty;

            var oldMap = oldInstances.ToDictionary(x => x.ServerInfo.Address);

            var newMap = newInstances
                .GroupBy(x => x.ServerInfo.Address)
                .ToDictionary(g => g.Key, g => g.Last());

            bool changed = false;

            foreach (var key in oldMap.Keys.Except(newMap.Keys).ToList())
            {
                oldMap.Remove(key);
                changed = true;
                _logger.LogDebug("Service {Service}: removed {Instance}", service, key);
            }

            foreach (var (key, newValue) in newMap)
            {
                if (!oldMap.TryGetValue(key, out var oldValue))
                {
                    oldMap[key] = newValue;
                    changed = true;
                    _logger.LogDebug("Service {Service}: added {Instance}", service, key);
                }
                else if (!AreEqual(oldValue, newValue))
                {
                    oldMap[key] = newValue;
                    changed = true;
                    _logger.LogDebug("Service {Service}: updated {Instance}", service, key);
                }
            }

            if (changed)
            {
                updates[service] = oldMap.Values.ToImmutableList();
            }
        }

        // удалённые сервисы
        foreach (var removedService in oldSnapshot.Keys.Except(newSnapshot.Keys))
        {
            ct.ThrowIfCancellationRequested();

            _logger.LogDebug("Service {Service}: removed completely", removedService);

            updates[removedService] = ImmutableList<ServerCondition>.Empty;
        }

        // батчевое применение
        if (updates.Count > 0)
        {
            _cache.ApplyBatch(updates, ct);
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
    /// Вычисляет, сколько подождать перед следующей попыткой запроса к registry.
    /// - чем больше ошибок подряд — тем дольше ждём (но не бесконечно)
    /// - добавляем случайность (jitter), чтобы не было синхронных пиков
    /// </summary>
    private TimeSpan CalculateDelay(int attempt)
    {
        var backoffSeconds = Math.Min(
            _maxBackoff.TotalSeconds,
            Math.Pow(2, attempt));
        
        var jitterMs = Random.Shared.Next(0, _jitterMaxMs);

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