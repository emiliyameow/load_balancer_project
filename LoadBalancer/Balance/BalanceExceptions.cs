namespace LoadBalancer.API.Balance;

/// <summary>
/// Базовое исключение для всех ошибок, связанных с процессом балансировки нагрузки.
/// </summary>
public class BalanceException : Exception
{
    public BalanceException(string message) : base(message) { }
    public BalanceException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Исключение, выбрасываемое при передаче некорректной или пустой коллекции серверов.
/// </summary>
public class InvalidServersCollectionException : BalanceException
{
    public InvalidServersCollectionException(string message) : base(message) { }
    public InvalidServersCollectionException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Исключение, выбрасываемое, когда в списке нет ни одного доступного сервера для обработки запроса.
/// </summary>
public class NoAliveServersException : BalanceException
{
    public NoAliveServersException(string message) : base(message) { }
    public NoAliveServersException(string message, Exception innerException) 
        : base(message, innerException) { }
}
