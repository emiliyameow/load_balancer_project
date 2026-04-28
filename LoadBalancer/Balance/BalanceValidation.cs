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

        if (servers.Any(s => s is null))
            throw new InvalidServersCollectionException("Servers list contains null elements.");

        var aliveServers = servers.Where(s => s.IsAlive).ToList();

        if (aliveServers.Count == 0)
            throw new NoAliveServersException("There are no alive servers.");

        if (aliveServers.Any(s => s.ServerInfo is null))
            throw new InvalidServersCollectionException("Server info is null.");

        if (aliveServers.Any(s => string.IsNullOrWhiteSpace(s.ServerInfo.Host)))
            throw new InvalidServersCollectionException("Server host is empty.");

        if (aliveServers.Any(s => s.ServerInfo.Port <= 0))
            throw new InvalidServersCollectionException("Server port is invalid.");

        if (aliveServers.Any(s => s.Weight <= 0))
            throw new InvalidServersCollectionException("Server weight must be positive.");

        return aliveServers;
    }

}