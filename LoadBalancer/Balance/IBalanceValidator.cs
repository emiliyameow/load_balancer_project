using LoadBalancer.API.HealthCheck;

namespace LoadBalancer.API.Balance;

/// <summary>
/// Интерфейс для валидации состояния серверов.
/// </summary>
public interface IBalanceValidator
{
    List<ServerCondition> GetValidatedAliveServers(List<ServerCondition> servers);
}