using LoadBalancer.API.HealthCheck;

namespace LoadBalancer.API.Balance;

public class BalanceAlgorithm : IBalanceAlgorithm
{
    private readonly object _strategyLock = new();
    private readonly ILogger<BalanceAlgorithm> _logger;
    private readonly IBalanceValidator _validator;
    private IBalanceStrategy _strategy;

    public BalanceAlgorithm(
        IBalanceStrategy strategy, 
        IBalanceValidator validator, 
        ILogger<BalanceAlgorithm> logger)
    {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("BalanceAlgorithm инициализирован со стратегией: {StrategyName}",
            _strategy.GetType().Name);
    }

    /// <summary>
    /// Изменяет текущую стратегию балансировки на новую.
    /// </summary>
    /// <param name="strategy">Новая реализация стратегии балансировки.</param>
    public void SetStrategy(IBalanceStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(strategy);
        string oldStrategyName;

        lock (_strategyLock)
        {
            oldStrategyName = _strategy.GetType().Name;
            _strategy = strategy;
        }

        _logger.LogWarning("Стратегия балансировки изменена с {OldStrategy} на {NewStrategy}",
            oldStrategyName, strategy.GetType().Name);
    }

    /// <summary>
    /// Выбирает свободный сервер из предоставленного списка, используя активную стратегию.
    /// </summary>
    /// <param name="servers">Список доступных серверов для анализа.</param>
    /// <returns>Экземпляр <see cref="ServerCondition"/>, выбранный текущим алгоритмом.</returns>
    public ServerCondition GetFreeServer(List<ServerCondition> servers)
    {
        var readyServers = _validator.GetValidatedAliveServers(servers);
        IBalanceStrategy activeStrategy;
        lock (_strategyLock) activeStrategy = _strategy;
        return activeStrategy.GetFreeServer(readyServers);
    }
}