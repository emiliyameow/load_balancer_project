using LoadBalancer.API.HealthCheck;

namespace LoadBalancer.API.Balance;

public class BalanceAlgoritm
{
    private readonly object _strategyLock = new();
    private IBalanceStrategy _strategy;

    public BalanceAlgoritm()
    {
        _strategy = new WeightedRoundRobinStrategy();
    }

    public BalanceAlgoritm(IBalanceStrategy strategy)
    {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
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
