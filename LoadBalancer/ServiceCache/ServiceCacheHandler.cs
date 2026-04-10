using LoadBalancer.API.HealthCheck;
using LoadBalancer.API.ServiceCache;
using System.Collections.Immutable;
using System.Threading;


namespace LoadBalancer.API.ServiceCache;

public class ServiceCacheHandler
{
    private ImmutableDictionary<string, ImmutableList<ServerCondition>> _cache
        = ImmutableDictionary<string, ImmutableList<ServerCondition>>.Empty;

    public IReadOnlyList<ServerCondition> GetInstances(string serviceName)
    {
        var snapshot = _cache;

        return snapshot.TryGetValue(serviceName, out var instances)
            ? instances
            : ImmutableList<ServerCondition>.Empty;
    }

    public IReadOnlyDictionary<string, ImmutableList<ServerCondition>> GetAll()
    {
        return _cache;
    }
    
    public void UpdateSnapshot(
        ImmutableDictionary<string, ImmutableList<ServerCondition>> newSnapshot)
    {
        Interlocked.Exchange(ref _cache, newSnapshot);
    }

    public void AddOrUpdateService(string service, ImmutableList<ServerCondition> instances)
    {
        while (true)
        {
            var snapshot = _cache;

            var updated = snapshot.SetItem(service, instances);

            var original = Interlocked.CompareExchange(ref _cache, updated, snapshot);

            if (ReferenceEquals(original, snapshot))
                return; // success
        }
    }
    public void RemoveService(string service)
    {
        while (true)
        {
            var snapshot = _cache;

            if (!snapshot.ContainsKey(service))
                return;

            var updated = snapshot.Remove(service);

            var original = Interlocked.CompareExchange(ref _cache, updated, snapshot);

            if (ReferenceEquals(original, snapshot))
                return;
        }
    }
    
}