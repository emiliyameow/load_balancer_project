namespace LoadBalancer.API.Balance;

public class BalanceStrategyRegistry
{
    private readonly Dictionary<string, IBalanceStrategy> _strategies;

    public BalanceStrategyRegistry(IEnumerable<IBalanceStrategy> strategies)
    {
        _strategies = new Dictionary<string, IBalanceStrategy>();

        foreach (var strategy in strategies)
        {
            if (_strategies.ContainsKey(strategy.Name))
            {
                throw new BalanceException(
                    $"Balance strategy '{strategy.Name}' already registered");
            }

            _strategies.Add(strategy.Name, strategy);
        }
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