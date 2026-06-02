using LoadBalancer.API.HealthCheck;

namespace LoadBalancer.API.Balance;

public class BalanceAlgorithm
{
    /// <summary>
    /// Объект синхронизации для потокобезопасной смены стратегии.
    /// </summary>
    private readonly object _strategyLock = new();

    /// <summary>
    /// Текущая активная стратегия балансировки.
    /// </summary>
    private IBalanceStrategy _strategy;

    /// <summary>
    /// Реестр всех доступных стратегий балансировки.
    /// </summary>
    private readonly BalanceStrategyRegistry _strategyRegistry;

    /// <summary>
    /// Создает экземпляр балансировщика
    /// с дефолтной стратегией минимального веса.
    /// </summary>
    public BalanceAlgorithm()
    {
        _strategy = new MinWeightStrategy();
        _strategyRegistry = new BalanceStrategyRegistry(
            new IBalanceStrategy[]
            {
                _strategy,
                new WeightedRoundRobinStrategy()
            });
    }

    /// <summary>
    /// Создает экземпляр балансировщика
    /// с использованием реестра стратегий.
    /// </summary>
    /// <param name="strategyRegistry">
    /// Реестр доступных стратегий балансировки.
    /// </param>
    /// <exception cref="BalanceException">
    /// Выбрасывается, если стратегия по умолчанию не зарегистрирована.
    /// </exception>
    public BalanceAlgorithm(BalanceStrategyRegistry strategyRegistry)
    {
        _strategyRegistry = strategyRegistry;

        if (!_strategyRegistry.TryGetStrategy("min-weight", out _strategy))
            throw new BalanceException("Default balancing algorithm was not registered");
    }

    /// <summary>
    /// Возвращает название текущего алгоритма балансировки.
    /// </summary>
    public string CurrentAlgorithm
    {
        get
        {
            lock (_strategyLock)
            {
                return _strategy.Name;
            }
        }
    }

    /// <summary>
    /// Пытается установить стратегию балансировки по имени.
    /// </summary>
    /// <param name="algorithm">
    /// Название алгоритма балансировки.
    /// </param>
    /// <returns>
    /// true — если стратегия успешно установлена;
    /// false — если стратегия не найдена.
    /// </returns>
    public bool TrySetStrategy(string algorithm)
    {
        if (!_strategyRegistry.TryGetStrategy(algorithm, out var strategy))
            return false;

        lock (_strategyLock)
        {
            _strategy = strategy;
        }

        return true;
    }

    /// <summary>
    /// Устанавливает новую стратегию балансировки.
    /// </summary>
    /// <param name="strategy">
    /// Экземпляр стратегии балансировки.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если strategy равен null.
    /// </exception>
    public void SetStrategy(IBalanceStrategy strategy)
    {
        if (strategy is null)
            throw new ArgumentNullException(nameof(strategy));

        lock (_strategyLock)
        {
            _strategy = strategy;
        }
    }

    /// <summary>
    /// Возвращает список доступных алгоритмов балансировки.
    /// </summary>
    /// <returns>
    /// Коллекция названий зарегистрированных алгоритмов.
    /// </returns>
    public IReadOnlyCollection<string> GetAvailableAlgorithms()
    {
        return _strategyRegistry.GetAvailableAlgorithms();
    }

    /// <summary>
    /// Возвращает свободный сервер
    /// с использованием текущей стратегии балансировки.
    /// </summary>
    /// <param name="servers">
    /// Список доступных серверов.
    /// </param>
    /// <returns>
    /// Свободный сервер.
    /// </returns>
    /// <exception cref="BalanceException">
    /// Выбрасывается при ошибке балансировки.
    /// </exception>
    public ServerCondition GetFreeServer(List<ServerCondition> servers)
    {
        try
        {
            IBalanceStrategy activeStrategy;

            lock (_strategyLock)
            {
                activeStrategy = _strategy;
            }

            return activeStrategy.GetFreeServer(servers);
        }
        catch (BalanceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BalanceException($"Unexpected balancing error: {ex.Message}");
        }
    }
}
