using LoadBalancer.API.HealthCheck;
using LoadBalancer.API.Rout;
using Microsoft.Extensions.Options;

namespace LoadBalancer.API.ServiceDiscovery;
/// <summary>
/// Fake Discovery Client - берет список серверов бекенда из конфига
/// </summary>
public class FakeServiceRegistry : IServiceRegistry
{
    private readonly Settings _settings;

    public FakeServiceRegistry(IOptions<Settings> options)
    {
        _settings = options.Value;
    }

    public Task<Dictionary<string, List<ServerCondition>>> GetServicesAsync()
    {
        var result = new Dictionary<string, List<ServerCondition>>();

        // пока один сервис
        var servers = _settings.Backends
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