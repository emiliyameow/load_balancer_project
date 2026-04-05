using LoadBalancer.API.ServiceCache;
using System.Collections.Immutable;
using System.Threading;

public class ServiceCache
{
    // snapshot (атомарно заменяется)
    private ImmutableDictionary<string, ImmutableList<ServiceInstance>> _cache
        = ImmutableDictionary<string, ImmutableList<ServiceInstance>>.Empty;

    // Получить все инстансы сервиса
    public IReadOnlyList<ServiceInstance> GetInstances(string serviceName)
    {
        var snapshot = _cache;

        return snapshot.TryGetValue(serviceName, out var instances)
            ? instances
            : ImmutableList<ServiceInstance>.Empty;
    }

    // Получить ВСЕ сервисы (иногда нужно)
    public IReadOnlyDictionary<string, ImmutableList<ServiceInstance>> GetAll()
    {
        return _cache;
    }

    // Полная замена snapshot (будем использовать позже)
    public void UpdateSnapshot(
        ImmutableDictionary<string, ImmutableList<ServiceInstance>> newSnapshot)
    {
        Interlocked.Exchange(ref _cache, newSnapshot);
    }
}