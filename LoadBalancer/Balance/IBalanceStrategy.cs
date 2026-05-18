using LoadBalancer.API.HealthCheck;

namespace LoadBalancer.API.Balance;

/// <summary>
/// Определяет стратегию выбора свободного сервера для распределения нагрузки.
/// </summary>
public interface IBalanceStrategy
{
    /// <summary>
    /// Выбирает подходящий сервер из списка доступных на основе логики конкретного алгоритма.
    /// </summary>
    ServerCondition GetFreeServer(List<ServerCondition> servers);
}
