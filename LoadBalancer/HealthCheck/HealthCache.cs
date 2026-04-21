using System.Collections.Concurrent;
using LoadBalancer.API.HealthCheck;

namespace LoadBalancer.API.HealthCheck;
/// <summary>
/// хранит именно адрес сервера + его состояние
/// </summary>
public class HealthCache
{
    // key = адрес сервера
    private readonly ConcurrentDictionary<string, bool> _health = new();

    public void Update(IEnumerable<ServerCondition> servers)
    {
        foreach (var server in servers)
        {
            var key = server.ServerInfo.Address;
            _health[key] = server.IsAlive;
        }
    }

    public bool IsHealthy(string address)
    {
        return _health.TryGetValue(address, out var isAlive) && isAlive;
    }
}