using LoadBalancer.API.HealthCheck;
using LoadBalancer.API.Rout;
using Microsoft.Extensions.Options;

namespace LoadBalancer.API.ServiceDiscovery;

public class FakeServiceRegistry : IServiceRegistry
{
    private readonly IOptionsMonitor<Settings> _settings;

    public FakeServiceRegistry(IOptionsMonitor<Settings> settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Получает список серверов из appsettings.json.
    /// </summary>
    public Task<Dictionary<string, List<ServerCondition>>> GetServicesAsync()
    {
        var current = _settings.CurrentValue;

        var result = new Dictionary<string, List<ServerCondition>>();

        var servers = current.Backends
            .Select(b => new ServerCondition
            {
                ServerInfo = b,
                IsAlive = true,
                Weight = 1
            })
            .ToList();

        result["users-service"] = servers;

        return Task.FromResult(result);
    }
}