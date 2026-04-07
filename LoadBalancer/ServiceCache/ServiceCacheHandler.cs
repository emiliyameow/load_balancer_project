using LoadBalancer.API.HealthCheck;
using LoadBalancer.API.ServiceCache;
using System.Collections.Immutable;
using System.Threading;

namespace LoadBalancer.API.ServiceCache;

public class ServiceCacheHandler
{
    // snapshot (атомарно заменяется)
    private ImmutableDictionary<string, ImmutableList<ServerCondition>> _cache
        = ImmutableDictionary<string, ImmutableList<ServerCondition>>.Empty;

    // Получить все инстансы сервиса
    public IReadOnlyList<ServerCondition> GetInstances(string serviceName)
    {
        var snapshot = _cache;

        return snapshot.TryGetValue(serviceName, out var instances)
            ? instances
            : ImmutableList<ServerCondition>.Empty;
    }

    // Получить ВСЕ сервисы (иногда нужно)
    public IReadOnlyDictionary<string, ImmutableList<ServerCondition>> GetAll()
    {
        return _cache;
    }

    // Полная замена snapshot (будем использовать позже)
    public void UpdateSnapshot(
        ImmutableDictionary<string, ImmutableList<ServerCondition>> newSnapshot)
    {
        Interlocked.Exchange(ref _cache, newSnapshot);
    }
}