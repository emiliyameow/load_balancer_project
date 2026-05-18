using LoadBalancer.API.HealthCheck;
using Microsoft.Extensions.Options;

namespace LoadBalancer.API.ServiceDiscovery;

public class FakeServiceRegistry(IOptionsMonitor<Settings> settings) : IServiceRegistry
{
    /// <summary>
    /// Получает список серверов из appsettings.json.
    /// </summary>
    public Task<Dictionary<string, List<ServerCondition>>> GetServicesAsync()
    {
        var current = settings.CurrentValue;

        var result = new Dictionary<string, List<ServerCondition>>();

        var servers = current.Backends
            .Select(b => new ServerCondition
            {
                ServerInfo = b,
                IsAlive = true,
                Weight = b.Weight
            })
            .ToList();

        result["users-service"] = servers;

        return Task.FromResult(result);
    }
}
