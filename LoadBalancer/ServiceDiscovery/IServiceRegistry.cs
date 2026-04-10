
using LoadBalancer.API.HealthCheck;

namespace LoadBalancer.API.ServiceDiscovery;

public interface IServiceRegistry
{
    Task<Dictionary<string, List<ServerCondition>>> GetServicesAsync();
}