
using LoadBalancer.API.HealthCheck;

namespace LoadBalancer.API.ServiceDiscovery;

/// <summary>
/// Интерфейс для реализации Service Discovery Client -
/// в случае замены списка серверов из файла конфигурации на UI - нужно имплементировать этот интерфейс.
/// </summary>
public interface IServiceRegistry
{
    Task<Dictionary<string, List<ServerCondition>>> GetServicesAsync(CancellationToken ctsToken);
}