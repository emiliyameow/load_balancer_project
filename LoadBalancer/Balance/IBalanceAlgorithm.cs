using LoadBalancer.API.HealthCheck;

namespace LoadBalancer.API.Balance;

/// <summary>
/// Интерфейс для контекста балансировки. 
/// Нужен для возможности подмены самого механизма выбора в DI и тестирования.
/// </summary>
public interface IBalanceAlgorithm
{
    void SetStrategy(IBalanceStrategy strategy);
    ServerCondition GetFreeServer(List<ServerCondition> servers);
}