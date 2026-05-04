using LoadBalancer.API.HealthCheck;

namespace LoadBalancer.API.Balance;

public class BalanceValidation
{
    public static List<ServerCondition> GetValidatedAliveServers(List<ServerCondition> servers)
    {
        if (servers is null)
            throw new InvalidServersCollectionException("Servers list is null.");
        if (servers.Count == 0)
            throw new InvalidServersCollectionException("Servers list is empty.");

        var aliveServers = new List<ServerCondition>(servers.Count);

        foreach (var server in servers)
        {
            if (server is null)
                throw new InvalidServersCollectionException("Servers list contains null elements.");

            if (!server.IsAlive)
                continue; // Пропускаем неживые сервера

            if (server.ServerInfo is null)
                throw new InvalidServersCollectionException("Server info is null.");
            if (string.IsNullOrWhiteSpace(server.ServerInfo.Host))

                throw new InvalidServersCollectionException("Server host is empty.");
            if (server.ServerInfo.Port <= 0)
                throw new InvalidServersCollectionException("Server port is invalid.");
            if (server.Weight <= 0)
                throw new InvalidServersCollectionException("Server weight must be positive.");

            aliveServers.Add(server);
        }

        if (aliveServers.Count == 0)
            throw new NoAliveServersException("There are no alive servers.");

        return aliveServers;
    }

}