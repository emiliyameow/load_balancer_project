namespace LoadBalancer.API.Balance;

public class BalanceStrategyRegistry
{
    private readonly Dictionary<string, IBalanceStrategy> _strategies;

    public BalanceStrategyRegistry(IEnumerable<IBalanceStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(
            strategy => strategy.Name,
            strategy => strategy,
            StringComparer.OrdinalIgnoreCase
        );
    }

    public bool TryGetStrategy(string algorithm, out IBalanceStrategy strategy)
    {
        strategy = null!;

        if (string.IsNullOrWhiteSpace(algorithm))
            return false;

        return _strategies.TryGetValue(algorithm.Trim(), out strategy!);
    }

    public IReadOnlyCollection<string> GetAvailableAlgorithms()
    {
        return _strategies.Keys.ToList();
    }
}