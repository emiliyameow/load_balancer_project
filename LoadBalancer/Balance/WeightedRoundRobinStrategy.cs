using LoadBalancer.API.HealthCheck;

namespace LoadBalancer.API.Balance;

public class WeightedRoundRobinStrategy : IBalanceStrategy
{

    public string Name => "weighted-round-robin";
    private readonly object _lock = new();
    private int _position;

    public ServerCondition GetFreeServer(List<ServerCondition> servers)
    {
        var aliveServers = BalanceValidation.GetValidatedAliveServers(servers);

        var totalWeight = aliveServers.Sum(s => s.Weight);

        lock (_lock)
        {
            if (_position >= totalWeight)
                _position = 0;

            var currentPosition = _position;
            _position = (_position + 1) % totalWeight;

            var accumulatedWeight = 0;
            foreach (var server in aliveServers)
            {
                accumulatedWeight += server.Weight;
                if (currentPosition < accumulatedWeight)
                    return server;
            }
        }

        return aliveServers[0];
    }
}
