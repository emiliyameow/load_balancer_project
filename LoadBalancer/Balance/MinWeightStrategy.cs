using LoadBalancer.API.HealthCheck;

namespace LoadBalancer.API.Balance;

/// <summary>
/// Стратегия выбора сервера с минимальным весом.
/// Позволяет направлять нагрузку на менее загруженные узлы в первую очередь.
/// </summary>
public class MinWeightStrategy : IBalanceStrategy
{
    public ServerCondition GetFreeServer(List<ServerCondition> servers)
    {
        if (servers == null || servers.Count == 0)
            throw new NoAliveServersException("Список серверов пуст");

        return servers
            .OrderBy(s => s.Weight)
            .ThenBy(s => s.ServerInfo.Name, StringComparer.OrdinalIgnoreCase)
            .First();
    }
}
