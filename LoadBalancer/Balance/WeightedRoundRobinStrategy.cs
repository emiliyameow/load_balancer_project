using LoadBalancer.API.HealthCheck;

namespace LoadBalancer.API.Balance;

/// <summary>
/// Стратегия взвешенного циклического перебора.
/// Распределяет запросы пропорционально весам серверов.
/// </summary>
public class WeightedRoundRobinStrategy : IBalanceStrategy
{

    public string Name => "weighted-round-robin";
    private readonly object _lock = new();
    private int _position;

    public ServerCondition GetFreeServer(List<ServerCondition> servers)
    {
        if (servers == null || servers.Count == 0)
            throw new NoAliveServersException("Список серверов пуст.");

        var weightedServers = servers
            .Where(s => s.Weight > 0)
            .ToList();

        if (weightedServers.Count == 0)
            throw new NoAliveServersException("Нет серверов с положительным весом.");

        var totalWeight = weightedServers.Sum(s => s.Weight);

        lock (_lock)
        {
            if (_position >= totalWeight) _position = 0;

            var currentPosition = _position;
            _position = (_position + 1) % totalWeight;

            var accumulatedWeight = 0;
            foreach (var server in weightedServers)
            {
                accumulatedWeight += server.Weight;
                if (currentPosition < accumulatedWeight) return server;
            }
        }

        return weightedServers[0];
    }
}
