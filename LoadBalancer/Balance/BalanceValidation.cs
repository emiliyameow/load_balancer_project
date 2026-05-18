using LoadBalancer.API.HealthCheck;

namespace LoadBalancer.API.Balance;

public class BalanceValidator : IBalanceValidator
{
    public List<ServerCondition> GetValidatedAliveServers(List<ServerCondition> servers)
    {
        if (servers is null || servers.Count == 0)
            throw new InvalidServersCollectionException("Список серверов не инициализирован");

        var aliveServers = new List<ServerCondition>();

        foreach (var server in servers)
        {
            if (server is null)
                throw new InvalidServersCollectionException("Список содержит пустые серверы");
            ValidateServerMetadata(server);
            if (server.IsAlive) aliveServers.Add(server);
        }

        if (aliveServers.Count == 0)
            throw new NoAliveServersException("Нет доступных серверов для обработки запроса.");

        return aliveServers;
    }

    private static void ValidateServerMetadata(ServerCondition server)
    {
        if (server.ServerInfo is null)
            throw new InvalidServersCollectionException("Отсутствует ServerInfo для сервера из списка");
        
        var serverName = server.ServerInfo.Name;

        if (string.IsNullOrWhiteSpace(server.ServerInfo.Host))
            throw new InvalidServersCollectionException($"Хост пустой для сервера {serverName}");

        if (server.ServerInfo.Port <= 0)
            throw new InvalidServersCollectionException($"Указан некорректный порт ({server.ServerInfo.Port}) для сервера {serverName}");

        if (server.Weight <= 0)
            throw new InvalidServersCollectionException($"Вес сервера {serverName} должен быть больше нуля (текущий: {server.Weight})");
    }
}