using LoadBalancer.API.HealthCheck;

namespace LoadBalancer.API.Balance;

public interface IBalanceStrategy
{
    string Name { get; }
    ServerCondition GetFreeServer(List<ServerCondition> servers);
}
