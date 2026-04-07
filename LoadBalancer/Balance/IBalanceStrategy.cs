using LoadBalancer.API.HealthCheck;

namespace LoadBalancer.API.Balance;

public interface IBalanceStrategy
{
    ServerCondition GetFreeServer(List<ServerCondition> servers);
}
