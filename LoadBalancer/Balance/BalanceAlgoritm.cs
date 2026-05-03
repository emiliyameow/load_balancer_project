using LoadBalancer.API.HealthCheck;

namespace LoadBalancer.API.Balance;

public class BalanceAlgoritm
{
    private readonly object _strategyLock = new();
    private IBalanceStrategy _strategy;
    private readonly BalanceStrategyRegistry _strategyRegistry;

    public BalanceAlgoritm()
    {
        _strategy = new WeightedRoundRobinStrategy();
    }

    public BalanceAlgoritm(BalanceStrategyRegistry strategyRegistry)
    {
        _strategyRegistry = strategyRegistry;

        if (!_strategyRegistry.TryGetStrategy("weighted-round-robin", out _strategy))
            throw new BalanceException("Default balancing algorithm was not registered");
    }

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

    public void SetStrategy(IBalanceStrategy strategy)
    {
        if (strategy is null)
            throw new ArgumentNullException(nameof(strategy));

        lock (_strategyLock)
        {
            _strategy = strategy;
        }
    }
    public IReadOnlyCollection<string> GetAvailableAlgorithms()
    {
        return _strategyRegistry.GetAvailableAlgorithms();
    }

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
