using LoadBalancer.API.HealthCheck;

namespace LoadBalancer.API.Balance;

/// <summary>
/// Стратегия взвешенного циклического перебора.
/// Распределяет запросы пропорционально весам серверов.
/// </summary>
public class WeightedRoundRobinStrategy : IBalanceStrategy
{
    private readonly object _lock = new();
    private int _position;

    public ServerCondition GetFreeServer(List<ServerCondition> servers)
    {
        if (servers == null || servers.Count == 0)
            throw new NoAliveServersException("Список серверов пуст.");

        var totalWeight = servers.Sum(s => s.Weight);

        lock (_lock)
        {
            if (_position >= totalWeight) _position = 0;

            var currentPosition = _position;
            _position = (_position + 1) % totalWeight;

            var accumulatedWeight = 0;
            foreach (var server in servers)
            {
                accumulatedWeight += server.Weight;
                if (currentPosition < accumulatedWeight) return server;
            }
        }

        return servers[0];
    }
}
