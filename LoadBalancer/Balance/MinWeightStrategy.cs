using LoadBalancer.API.HealthCheck;

namespace LoadBalancer.API.Balance;

public class MinWeightStrategy : IBalanceStrategy
{

    public string Name => "min-weight";
    public ServerCondition GetFreeServer(List<ServerCondition> servers)
    {
       var aliveServers = BalanceValidation.GetValidatedAliveServers(servers);

        return aliveServers
            .OrderBy(s => s.Weight)
            .ThenBy(s => s.ServerInfo.Name, StringComparer.OrdinalIgnoreCase)
            .First();
    }
}
