namespace LoadBalancer.API.Balance;

public class BalanceException : Exception
{
    public BalanceException(string message) : base(message) { }
}

public class InvalidServersCollectionException : BalanceException
{
    public InvalidServersCollectionException(string message) : base(message) { }
}

public class NoAliveServersException : BalanceException
{
    public NoAliveServersException(string message) : base(message) { }
}
