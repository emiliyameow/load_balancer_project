namespace LoadBalancer.API.Balance;

public interface IBalanceStrategy
{
    ServerCondition GetFreeServer(List<ServerCondition> servers);
}
